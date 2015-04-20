using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Threading.Tasks;
using NUnit.Framework;
using QuickGraph;

namespace AccessControlGraph.Tests
{
    /// <summary>
    /// These tests are for performance
    /// </summary>
    [TestFixture]
    public class ACGThreading
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
                var actions = new List<Action>();

                for (var i = 1; i < 10; i++)
                {
                    for (var j = 1; j < 10; j++)
                    {
                        var ii = i;
                        var jj = j;
                        Action addAction = () =>
                        {
                            var edges = new List<Edge<TestNode>>();
                            edges.Add(new Edge<TestNode>(new TestNode(ii), new TestNode(ii*10 + jj)));
                            for (var k = 1; k < 10; k++)
                            {
                                edges.Add(new Edge<TestNode>(new TestNode(ii*10 + jj), new TestNode(ii*100 + jj*10 + k)));
                                for (var q = 1; q < 10; q++)
                                    edges.Add(new Edge<TestNode>(new TestNode(ii*100 + jj*10 + k),
                                        new TestNode(ii*1000 + jj*100 + k*10 + q)));
                            }
                            _graph.AddVerticesAndEdgeRange(edges);
                        };
                        actions.Add(addAction);
                    }
                }
                var tasks = actions.ConvertAll(Task.Run);
                Task.WaitAll(tasks.ToArray());
                                
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
        public void create_graph()
        {
            
        }

        [Test]
        public void create_subgraph()
        {
            Action op = () =>
            {
                var tasks = Enumerable.Range(1,9).ToList().ConvertAll( i => Task.Run(() =>
                {
                    Expression<Func<int, VertexPredicate<TestNode>>> expr = x => node => node.Id.ToString().StartsWith(x.ToString());
                    var rev = new ReplaceExpressionVisitor();
                    rev.Replaces["x"] = i;
                    var replacedExpr = rev.Visit(expr.Body);
                    var resultExpr = (Expression<VertexPredicate<TestNode>>)replacedExpr;

                    var cg = _graph.GetChildGraph(resultExpr);    
                }));
                Task.WaitAll(tasks.ToArray());                
            };            
            var notcached = MeasureTimeSpan(op); 
            var cached = MeasureTimeSpan(op);
        }

        [Test]
        public void delete_nodes()
        {
            Action op = () =>
            {
                var tasks = Enumerable.Range(1, 9999).Where(i=> i % 2 == 1).ToList().ConvertAll(i => Task.Run(() => _graph.RemoveVertex(new TestNode(i))));
                Task.WaitAll(tasks.ToArray());
            };
            var delete = MeasureTimeSpan(op); //00:00:00.02
            Assert.That(_graph.Vertices.Count() == 3280); //mathematically counted to right amount if there was deleted all odd values;
            Assert.That(_graph.Edges.Count() == 1456);

            Action op2 = () =>
            {
                var tasks = Enumerable.Range(1, 9999).Where(i => i % 2 == 0).ToList().ConvertAll(i => Task.Run(() => _graph.RemoveVertex(new TestNode(i))));
                Task.WaitAll(tasks.ToArray());
            };
            var delete2 = MeasureTimeSpan(op2); //00:00:00.01
            Assert.That(!_graph.Vertices.Any());
            Assert.That(!_graph.Edges.Any());
        }

        [Test]
        public void delete_nodes_from_tree_with_subrees()
        {
            create_subgraph();

            Action op = () =>
            {
                var tasks = Enumerable.Range(1, 9999).Where(i => i % 2 == 1).ToList().ConvertAll(i => Task.Run(() => _graph.RemoveVertex(new TestNode(i))));
                Task.WaitAll(tasks.ToArray());
            };
            var delete = MeasureTimeSpan(op); //00:00:00.010
            Assert.That(_graph.Vertices.Count() == 3280); //mathematically counted to right amount if there was deleted all odd values;
            Assert.That(_graph.Edges.Count() == 1456);

            var count = _graph.Cache.Values.Sum(cacheValue => cacheValue.Graph.Vertices.Count());
            Assert.That(count == 3280, "all subgraphs must contain same vertexes count as parent graph"); 
        }
    }
}
