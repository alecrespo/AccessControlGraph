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
        //TODO: Сделать чтобы со временем кеш протухал и удалялся.
        internal readonly Dictionary<string, CacheValue<T>> Cache = new Dictionary<string, CacheValue<T>>();       

        public AccessControlGraphRoot()
        {
            Graph.VertexAdded += vertex =>
            {
                //make sure that handler is there
                vertex.PropertyChanged -= vertex_PropertyChanged;
                vertex.PropertyChanged += vertex_PropertyChanged;

                foreach (var cacheValue in Cache)
                    if (cacheValue.Value.Predicate(vertex))
                        cacheValue.Value.Graph.Graph.AddVertex(vertex);
            };

            Graph.VertexRemoved += vertex =>
            {
                vertex.PropertyChanged -= vertex_PropertyChanged;

                foreach (var cacheValue in Cache)
                    if (cacheValue.Value.Graph.Graph.ContainsVertex(vertex))
                        cacheValue.Value.Graph.Graph.RemoveVertex(vertex);
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
                        cacheValue.Value.Graph.Graph.AddVerticesAndEdge(edge);
            };

            Graph.EdgeRemoved += edge =>
            {
                edge.Source.PropertyChanged -= vertex_PropertyChanged;
                edge.Target.PropertyChanged -= vertex_PropertyChanged;

                //if edge removed from RoogGraph, delete it from all childs.
                foreach (var cacheValue in Cache)
                    cacheValue.Value.Graph.Graph.RemoveEdge(edge);
            };            
        }

        void vertex_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var node = (T) sender;
            foreach (var cacheValue in Cache)
            {
                var childGraph = cacheValue.Value.Graph.Graph;
                var predicate = cacheValue.Value.Predicate;

                var childContains = childGraph.ContainsVertex(node);
                var nodeMatchPredicate = predicate(node);

                if (childContains && !nodeMatchPredicate) //delete node from acg with edges
                    childGraph.RemoveVertex(node);

                if (!childContains && nodeMatchPredicate) //add node to acg with edges (from parent graph)
                    childGraph.AddVerticesAndEdgeRange(
                        Graph.AdjacentEdges(node).Where(edge => predicate(edge.GetOtherVertex(node)))
                        );
            }
        }

        /// <summary>
        /// Добавить рёбра в граф, а также вершины, которые соединяют эти рёбра.
        /// </summary>
        public int AddVerticesAndEdgeRange(IEnumerable<Edge<T>> edges)
        {
            return Graph.AddVerticesAndEdgeRange(edges);
        }

        /// <summary>
        /// Удалить вершину из графа, а также все рёбра с ней связанные.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public void RemoveVertex(T v)
        {
            if(Graph.ContainsVertex(v))
                Graph.RemoveVertex(v);
        }

        /// <summary>
        /// Получить подграф, вершины которого удовлетворяют условию.
        /// возвращаемые подграфы кешируются по LINQ expression.
        /// </summary>
        /// <returns>Подграф, доступны операции только для чтения</returns>
        public AccessControlGraph<T> GetChildGraph(Expression<VertexPredicate<T>> v)
        {
            if (Cache.ContainsKey(v.ToString()))
                return Cache[v.ToString()].Graph;
            var predicate = v.Compile();

            //clone parent graph
            var acg = new AccessControlGraph<T>();
            acg.Graph.AddVerticesAndEdgeRange(Edges.ToList());

            //filter graph by predicate
            acg.Graph.RemoveVertexIf(x => !predicate(x));

            Cache.Add(v.ToString(), new CacheValue<T>
            {
                Graph = acg, 
                Predicate = predicate
            });

            return acg;
        }
    }
}
