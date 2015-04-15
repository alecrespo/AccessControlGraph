using System.Collections.Generic;
using QuickGraph;

namespace AccessControlGraph
{
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
