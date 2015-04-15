using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;

namespace AccessControlGraph
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AccessControlGraphRoot<T> : AccessControlGraph<T>
        where T : NodeBase
    {
        //TODO: Сделать чтобы со временем кеш протухал и удалялся.
        internal readonly Dictionary<VertexPredicate<T>, AccessControlGraph<T>> Cache = new Dictionary<VertexPredicate<T>, AccessControlGraph<T>>();       

        public AccessControlGraphRoot()
        {            
            Graph.EdgeAdded += e => Cache.ToList().ForEach(acg =>
            {
                if(acg.Key(e.Source) && acg.Key(e.Target))
                    acg.Value.Graph.AddVerticesAndEdge(e);
            });
            Graph.EdgeRemoved += e => Cache.ToList().ForEach(acg =>
            {
                acg.Value.Graph.RemoveEdge(e);                
            });
            Graph.VertexAdded += v => Cache.ToList().ForEach(acg =>
            {
                if(acg.Key(v)) 
                    acg.Value.Graph.AddVertex(v);
            });
            Graph.VertexRemoved += v => Cache.ToList().ForEach(acg =>
            {
                acg.Value.Graph.RemoveVertex(v);
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
        public AccessControlGraph<T> GetChildGraph(VertexPredicate<T> v)
        {
            if (Cache.ContainsKey(v))
                return Cache[v];

            //clone parent graph
            var acg = new AccessControlGraph<T>();
            acg.Graph.AddVerticesAndEdgeRange(Edges.ToList());

            //filter graph by predicate
            acg.Graph.RemoveVertexIf(x => !v(x));

            Cache.Add(v, acg);

            return acg;
        }
    }
}
