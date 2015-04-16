using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using QuickGraph;

namespace AccessControlGraph.Tests
{
    /// <summary>
    /// These tests are for performance
    /// </summary>
    [TestFixture]
    public class ACGPerformance
    {
        private AccessControlGraphRoot<TestNode> _graph;

        public TimeSpan MeasureTimeSpan(Action act)
        {
            var sw = new Stopwatch();
            sw.Start();
            act();
            return sw.Elapsed;
        }

        [SetUp]
        public void SetUp()
        {
            Action act = () =>{
                //init graph with tree-like structure, so it will be easy to test operations on this graph.
                _graph = new AccessControlGraphRoot<TestNode>();
                var edges = new List<Edge<TestNode>>();

                for (var i = 1; i < 10; i++)
                {
                    for (var j = 1; j < 10; j++)
                    {
                        edges.Add(new Edge<TestNode>(new TestNode(i), new TestNode(i * 10 + j)));
                        for (var k = 1; k < 10; k++)
                        {
                            edges.Add(new Edge<TestNode>(new TestNode(i * 10 + j), new TestNode(i * 100 + j * 10 + k)));
                            for (var q = 1; q < 10; q++)
                                edges.Add(new Edge<TestNode>(new TestNode(i * 100 + j * 10 + k), new TestNode(i * 1000 + j * 100 + k * 10 + q)));
                        }
                    }
                }
                _graph.AddVerticesAndEdgeRange(edges);
                Assert.That(_graph.Vertices.Count() == 9 + 9 * 9 + 9 * 9 * 9 + 9 * 9 * 9 * 9); //9 on each level of tree(4 levels)  =  7380 nodex
                Assert.That(_graph.Edges.Count() == 9 * 9 + 9 * 9 * 9 + 9 * 9 * 9 * 9); //edges 7371
            };
            var creation = MeasureTimeSpan(act); //00.00.00.06 - 00.00.00.08
        }

        [TearDown]
        public void TearDown()
        {
            _graph = null;
        }

        [Test]
        public void create_graph_time()
        {
            
        }

        [Test]
        public void create_subgraph_time()
        {
            Action op = () =>
            {
                for (var i = 1; i < 10; i++)
                {
                    Expression<Func<int, VertexPredicate<TestNode>>> expr = x => node => node.Id.ToString().StartsWith(x.ToString());
                    var rev = new ReplaceExpressionVisitor();
                    rev.Replaces["x"] = i;
                    var replacedExpr = rev.Visit(expr.Body);
                    var resultExpr = (Expression<VertexPredicate<TestNode>>)replacedExpr;

                    var cg = _graph.GetChildGraph(resultExpr);
                }
            };
            var notcached = MeasureTimeSpan(op); //00:00:00.200
            var cached = MeasureTimeSpan(op); //00:00:00.001
        }

        [Test]
        public void delete_nodes_time()
        {
            Action op = () =>
            {
                for (var i = 1; i < 10000; i+=2)
                    _graph.RemoveVertex(new TestNode(i));
            };            
            var delete = MeasureTimeSpan(op); //00:00:00.006
            Assert.That(_graph.Vertices.Count() == 3280); //mathematically counted to right amount if there was deleted all odd values;
            Assert.That(_graph.Edges.Count() == 1456);

            Action op2 = () =>
            {
                for (var i = 2; i < 10000; i += 2)
                    _graph.RemoveVertex(new TestNode(i));
            };
            var delete2 = MeasureTimeSpan(op2); //00:00:00.001
            Assert.That(!_graph.Vertices.Any()); 
            Assert.That(!_graph.Edges.Any());
        }

        [Test]
        public void delete_nodes_from_tree_with_subrees_time()
        {
            create_subgraph_time();

            Action op = () =>
            {
                for (var i = 1; i < 10000; i+=2)
                    _graph.RemoveVertex(new TestNode(i));
            };            
            var delete = MeasureTimeSpan(op); //00:00:00.010
            Assert.That(_graph.Vertices.Count() == 3280); //mathematically counted to right amount if there was deleted all odd values;
            Assert.That(_graph.Edges.Count() == 1456);

            var count = 0;
            foreach (var cacheValue in _graph.Cache.Values)
                count += cacheValue.Graph.Vertices.Count();
            Assert.That(count == 3280); //all subgraphs contains same vertexes count as parent graph;
        }
    }
}
