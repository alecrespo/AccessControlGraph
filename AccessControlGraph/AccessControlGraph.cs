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
        where T:NodeBase
    {
        readonly Dictionary<VertexPredicate<T>,AccessControlGraph<T>> _cache = new Dictionary<VertexPredicate<T>, AccessControlGraph<T>>();

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
            if (_cache.ContainsKey(v))
                return _cache[v];
            
            //clone parent graph
            var acg = new AccessControlGraph<T>();
            acg.Graph.AddVerticesAndEdgeRange(Edges.ToList());
            
            //filter graph by predicate
            acg.Graph.RemoveVertexIf(x => !v(x));
            
            _cache.Add(v, acg);

            return acg;
        }
    }

    /// <summary>
    /// Класс подграфа существующего AccessControlGraphRoot
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AccessControlGraph<T> 
        where T : NodeBase
    {
        internal readonly UndirectedGraph<T, Edge<T>> Graph = new UndirectedGraph<T, Edge<T>>(false);

        /// <summary>
        /// Запрет инстанцирования напрямую
        /// </summary>
        internal AccessControlGraph()
        {
            
        } 

        /// <summary>
        /// Все вершины графа
        /// </summary>
        public IEnumerable<T> Vertices
        {
            get { return Graph.Vertices; }
        }

        /// <summary>
        /// Все рёбра графа
        /// </summary>
        public IEnumerable<Edge<T>> Edges
        {
            get { return Graph.Edges; }
        }

        /// <summary>
        /// Получить все рёбра указанной вершины
        /// </summary>
        public IEnumerable<Edge<T>> AdjacentEdges(T v)
        {
            return Graph.AdjacentEdges(v);
        }

    }
}
