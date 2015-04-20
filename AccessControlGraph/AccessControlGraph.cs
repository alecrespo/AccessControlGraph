using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using QuickGraph;

namespace AccessControlGraph
{
    /// <summary>
    /// Класс подграфа существующего AccessControlGraphRoot
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AccessControlGraph<T> 
        where T : NodeBase, INotifyPropertyChanged
    {
        internal readonly UndirectedGraph<T, Edge<T>> Graph = new UndirectedGraph<T, Edge<T>>(false);

        protected readonly object GraphLocker = new object();

        /// <summary>
        /// Запрет инстанцирования напрямую
        /// </summary>
        internal AccessControlGraph()
        {
            
        }

        #region Thread safe graph operations

        /// <summary>
        /// Все вершины графа
        /// </summary>
        public IEnumerable<T> Vertices
        {
            get
            {
                lock (GraphLocker)
                    return Graph.Vertices.ToList();
            }
        }

        /// <summary>
        /// Все рёбра графа
        /// </summary>
        public IEnumerable<Edge<T>> Edges
        {
            get
            {
                lock(GraphLocker)
                    return Graph.Edges.ToList();
            }
        }

        /// <summary>
        /// Получить все рёбра указанной вершины
        /// </summary>
        public IEnumerable<Edge<T>> AdjacentEdges(T v)
        {
            lock (GraphLocker)
                return Graph.AdjacentEdges(v);
        }        

        /// <summary>
        /// Удалить вершину из графа, а также все рёбра с ней связанные.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal bool RemoveVertex(T v)
        {
            lock (GraphLocker)
                return Graph.RemoveVertex(v);
        }

        /// <summary>
        /// Добавить вершину
        /// </summary>
        /// <param name="v">True если добавилась</param>
        /// <returns></returns>
        internal bool AddVertex(T v)
        {
            lock (GraphLocker)
                return Graph.AddVertex(v);
        }

        /// <summary>
        /// Содержится ли вершина?
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal bool ContainsVertex(T v)
        {
            lock (GraphLocker)
                return Graph.ContainsVertex(v);
        }

        /// <summary>
        /// Попытаться удалить вершину
        /// </summary>
        /// <param name="v">True если вершина была и удалилась, False в других случаях</param>
        /// <returns></returns>
        internal bool TryRemoveVertex(T v)
        {
            lock (GraphLocker)
                return Graph.ContainsVertex(v) && Graph.RemoveVertex(v);
        }

        /// <summary>
        /// Удалить грань
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal bool RemoveEdge(Edge<T> e)
        {
            lock (GraphLocker)
                return Graph.RemoveEdge(e);
        }

        /// <summary>
        /// Добавить грань и прилегающие к ней вершины в граф
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal bool AddVerticesAndEdge(Edge<T> e)
        {
            lock (GraphLocker)
                return Graph.AddVerticesAndEdge(e);
        }

        /// <summary>
        /// Добавить грани и прилегающие к ним вершины в граф
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        internal int AddVerticesAndEdgeRange(IEnumerable<Edge<T>> edges)
        {
            lock (GraphLocker)
                return Graph.AddVerticesAndEdgeRange(edges);
        }

        #endregion
    }
}
