using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using QuickGraph;

namespace AccessControlGraph
{
    internal class CacheValue<T> where T : NodeBase
    {
        public VertexPredicate<T> Predicate { get; set; }
        public AccessControlGraph<T> Graph { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AccessControlGraphRoot<T> : AccessControlGraph<T>
        where T : NodeBase
    {
        //TODO: Сделать чтобы со временем кеш протухал и удалялся.
        internal readonly Dictionary<string, CacheValue<T>> Cache = new Dictionary<string, CacheValue<T>>();       

        public AccessControlGraphRoot()
        {
            Graph.EdgeAdded += e => Cache.ToList().ForEach(acg =>
            {
                if (acg.Value.Predicate(e.Source) && acg.Value.Predicate(e.Target))
                    acg.Value.Graph.Graph.AddVerticesAndEdge(e);
            });
            Graph.EdgeRemoved += e => Cache.ToList().ForEach(acg =>
            {
                acg.Value.Graph.Graph.RemoveEdge(e);
            });
            Graph.VertexAdded += v => Cache.ToList().ForEach(acg =>
            {
                if (acg.Value.Predicate(v))
                    acg.Value.Graph.Graph.AddVertex(v);
            });
            Graph.VertexRemoved += v => Cache.ToList().ForEach(acg =>
            {
                if(acg.Value.Graph.Graph.ContainsVertex(v))
                    acg.Value.Graph.Graph.RemoveVertex(v);
            });
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
        public bool RemoveVertex(T v)
        {
            return Graph.RemoveVertex(v);
        }

        /// <summary>
        /// Получить подграф, вершины которого удовлетворяют условию.
        /// возвращаемые подграфы кешируются по предикату.
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
