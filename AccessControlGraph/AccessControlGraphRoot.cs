using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using QuickGraph;

namespace AccessControlGraph
{
    internal class CacheValue<T> where T : NodeBase, INotifyPropertyChanged
    {
        public VertexPredicate<T> Predicate { get; set; }
        public AccessControlGraph<T> Graph { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AccessControlGraphRoot<T> : AccessControlGraph<T>
        where T : NodeBase, INotifyPropertyChanged
    {
        //TODO: Сделать чтобы со временем кеш протухал и удалялся. Возможно использовать MemoryCache
        internal readonly ConcurrentDictionary<string, CacheValue<T>> Cache = new ConcurrentDictionary<string, CacheValue<T>>();

        public AccessControlGraphRoot()
        {
            //these event handlers are already in GraphLocker lock, as them called from locked methods from AccessControlGraph<T>.

            Graph.VertexAdded += vertex =>
            {
                //make sure that handler is there
                vertex.PropertyChanged -= vertex_PropertyChanged;
                vertex.PropertyChanged += vertex_PropertyChanged;

                foreach (var cacheValue in Cache.Where(element => element.Value.Predicate(vertex)))
                    cacheValue.Value.Graph.AddVertex(vertex);
            };

            Graph.VertexRemoved += vertex =>
            {             
                vertex.PropertyChanged -= vertex_PropertyChanged;

                foreach (var cacheValue in Cache)
                    cacheValue.Value.Graph.TryRemoveVertex(vertex);                    
            };

            Graph.EdgeAdded += edge =>
            {                
                //make sure that handler is there
                edge.Source.PropertyChanged -= vertex_PropertyChanged;
                edge.Source.PropertyChanged += vertex_PropertyChanged;
                edge.Target.PropertyChanged -= vertex_PropertyChanged;
                edge.Target.PropertyChanged += vertex_PropertyChanged;

                //if edge(and its nodes) right for this childgraph, adding it.
                foreach (var cacheValue in Cache)
                    if (cacheValue.Value.Predicate(edge.Source) && cacheValue.Value.Predicate(edge.Target))
                        cacheValue.Value.Graph.AddVerticesAndEdge(edge);
            };

            Graph.EdgeRemoved += edge =>
            {
                edge.Source.PropertyChanged -= vertex_PropertyChanged;
                edge.Target.PropertyChanged -= vertex_PropertyChanged;

                //if edge removed from RoogGraph, delete it from all childs.
                foreach (var cacheValue in Cache)
                    cacheValue.Value.Graph.RemoveEdge(edge);
            };            
        }

        void vertex_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var node = (T)sender;
            
            lock (GraphLocker)
                foreach (var cacheValue in Cache)
                {
                    var childGraph = cacheValue.Value.Graph;
                    var predicate = cacheValue.Value.Predicate;

                    var childContains = childGraph.ContainsVertex(node);
                    var nodeMatchPredicate = predicate(node);

                    if (childContains && !nodeMatchPredicate) //delete node from acg with edges
                        childGraph.RemoveVertex(node);

                    if (!childContains && nodeMatchPredicate) //add node to acg with edges (from parent graph)
                        childGraph.AddVerticesAndEdgeRange(
                            AdjacentEdges(node).Where(edge => predicate(edge.GetOtherVertex(node)))
                            );
                }        
        }

        /// <summary>
        /// Добавить рёбра в граф, а также вершины, которые соединяют эти рёбра.
        /// </summary>
        public new int AddVerticesAndEdgeRange(IEnumerable<Edge<T>> edges)
        {
            return base.AddVerticesAndEdgeRange(edges);
        }

        /// <summary>
        /// Удалить вершину из графа, а также все рёбра с ней связанные.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public new void RemoveVertex(T v)
        {            
            lock (GraphLocker)
                if(ContainsVertex(v))
                    base.RemoveVertex(v);
        }

        /// <summary>
        /// Получить подграф, вершины которого удовлетворяют условию.
        /// возвращаемые подграфы кешируются по LINQ expression.
        /// </summary>
        /// <returns>Подграф, доступны операции только для чтения</returns>
        public AccessControlGraph<T> GetChildGraph(Expression<VertexPredicate<T>> v)
        {
            CacheValue<T> ret;
            if (Cache.TryGetValue(v.ToString(), out ret))
                return ret.Graph;

            var predicate = v.Compile();

            //locking so we cant add new edge or vertex, as it brokes the new subgraph consistency.
            lock (GraphLocker) 
            {
                //clone parent graph
                var edges = Graph.Edges.ToList();
                var acg = new AccessControlGraph<T>();
                acg.Graph.AddVerticesAndEdgeRange(edges);

                //filter graph by predicate
                acg.Graph.RemoveVertexIf(x => !predicate(x));

                var cvalue = new CacheValue<T>
                {
                    Graph = acg,
                    Predicate = predicate
                };
                return Cache.GetOrAdd(v.ToString(), cvalue).Graph;
            }
        }
    }
}
