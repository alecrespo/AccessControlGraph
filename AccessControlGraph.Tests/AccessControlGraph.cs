using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using QuickGraph;
using Assert = NUnit.Framework.Assert;

namespace AccessControlGraph.Tests
{
    [TestFixture]
    public class AccessControlGraph
    {
        private AccessControlGraphRoot<TestNode> _graph;
            
        [SetUp]
        public void SetUp()
        {
            //init graph with tree-like structure, so it will be easy to test operations on this graph.
            _graph = new AccessControlGraphRoot<TestNode>();
            //Adding to node subnodes from start to start+count;
            
            Action<int, int, int> add = (node, start, count) =>
            {
                var n = new TestNode(node);
                _graph.AddVerticesAndEdgeRange(
                    Enumerable.Range(start, count).ToList().ConvertAll(
                        i => new Edge<TestNode>(n, new TestNode(i))
                        )
                    );
            };
            add(1, 11, 4);
            add(11, 111, 4);
            add(12, 121, 4);
            add(13, 131, 4);
            add(14, 141, 4);

            //now we have 3-level tree with 4 subnodes on each node;
            //21 nodes, 20 edges
            Assert.True(_graph.Vertices.Count() == 21);
            Assert.True(_graph.Edges.Count() == 20);
        }

        [TearDown]
        public void TearDown()
        {
            _graph = null;
        }
        [Test]
        public void subgaph_creation()
        {
            //trying various graph filtering scheme
            var subgraph = _graph.GetChildGraph(v => v.Id > 11);
            Assert.True(subgraph.Vertices.Count() == 19);
            Assert.True(subgraph.Edges.Count() == 12);

            var subgraph2 = _graph.GetChildGraph(v => v.Id % 2 == 0);
            Assert.True(subgraph2.Vertices.Count() == 10);
            Assert.True(subgraph2.Edges.Count() == 4);

            var subgraph3 = _graph.GetChildGraph(v => v.Id % 2 == 1);
            Assert.True(subgraph3.Vertices.Count() == 11);
            Assert.True(subgraph3.Edges.Count() == 6);
        }

        [Test]
        public void graph_and_subgraph_using_same_node_objects()
        {
            //change data before subgraph creation
            _graph.Vertices.First(x => x.Id == 12).Testdata = "12";

            var subgraph = _graph.GetChildGraph(v => v.Id % 2 == 0);

            //change data after subgraph creation 
            _graph.Vertices.First(x => x.Id == 122).Testdata = "122";

            //make requests for subgraph nodes data;
            Assert.True(subgraph.Vertices.First(x => x.Id == 12).Testdata == "12");
            Assert.True(subgraph.Vertices.First(x => x.Id == 122).Testdata == "122");
        }

        [Test]
        [Description("Check valid cache hit once.")]
        public void check_cache()
        {
            Expression<VertexPredicate<TestNode>> testFunc = v => v.Id % 2 == 0;
            var subgraph = _graph.GetChildGraph(testFunc);

            var cachedsubgraph = _graph.GetChildGraph(testFunc);
            Assert.True(subgraph.Equals(cachedsubgraph));

            //as you can see cache works even with virtually same anonimous function
            //implicitly converts function to expression, end they are the same.
            var uncachedsubgraph = _graph.GetChildGraph(v => v.Id % 2 == 0); 
            Assert.True(subgraph.Equals(uncachedsubgraph));
        }

        [Test]
        [Description("Check valid cache hit with variable param.")]
        public void child_graph_cached_on_predicate_result()
        {
            for (var j = 0; j < 2; j++) //run two times
                for (var i = 2; i < 10; i++) //8 subtree calls each time
                {
                    Expression<Func<int, int, VertexPredicate<TestNode>>> expr = (x,y) => node => node.Id > x*y;
                    var visitor = new ReplaceExpressionVisitor();
                    visitor.Replaces["x"] = i;
                    visitor.Replaces["y"] = i;
                    var replacedExpr = visitor.Visit(expr.Body);
                    var resultExpr = (Expression<VertexPredicate<TestNode>>)replacedExpr;
                    
                    var testCount = _graph.GetChildGraph(resultExpr).Vertices.Count() ==
                                    _graph.Vertices.Count(n => n.Id > i*i);
                    Assert.That(testCount,"Subgraph contains wrong number of Vertices when selection based on predicate.");
                }
            Assert.That(_graph.Cache.Count==8,"Caching done wrong based on predicate result");
        }


        [Test]
        public void data_updates_on_insert()
        {
            var a = new VertexPredicate<TestNode>(v => v.Id % 2 == 1);


            var subgraph = _graph.GetChildGraph(v => v.Id % 2 == 0);
            var subgraph2 = _graph.GetChildGraph(v => v.Id % 2 == 1);

            _graph.Vertices.First(x => x.Id == 12).Testdata = "12";
            //insert
            var list = new List<Edge<TestNode>>();
            var node1 = new TestNode(12) { Testdata = "13" };  //MUST NOT UPDATE WITH NEW DATA
            var node2 = new TestNode(126) { Testdata = "126" };
            var edge = new Edge<TestNode>(node1, node2);
            list.Add(edge);
            _graph.AddVerticesAndEdgeRange(list);

            Assert.True(_graph.Vertices.Count() == 22);
            Assert.True(_graph.Edges.Count() == 21);
            Assert.True(subgraph.Vertices.Count() == 11);
            Assert.True(subgraph.Edges.Count() == 5);
            Assert.True(subgraph2.Vertices.Count() == 11);
            Assert.True(subgraph2.Edges.Count() == 6);

            Assert.True(_graph.Vertices.First(x => x.Id == 126).Testdata == "126");
            Assert.True(subgraph.Vertices.First(x => x.Id == 126).Testdata == "126");
            Assert.True(subgraph2.Vertices.Count(x => x.Id == 126) == 0);

            Assert.True(_graph.Vertices.First(x => x.Id == 12).Testdata == "12");
            Assert.True(subgraph.Vertices.First(x => x.Id == 12).Testdata == "12");
            Assert.True(subgraph2.Vertices.Count(x => x.Id == 12) == 0);
        }

        //[Test]
        //public void data_updates_on_delete()
        //{
        //    var subgraph = graph.GetChildGraph(v => v.Id % 2 == 0);
        //    var subgraph2 = graph.GetChildGraph(v => v.Id % 2 == 1);

        //    graph.Vertices.First(x => x.Id == 12).Testdata = "12";
        //    //insert
        //    var list = new List<Edge<TestNode>>();
        //    var node1 = new TestNode(12) { Testdata = "13" };  //MUST NOT UPDATE WITH NEW DATA
        //    var node2 = new TestNode(126) { Testdata = "126" };
        //    var edge = new Edge<TestNode>(node1, node2);
        //    list.Add(edge);
        //    graph.AddVerticesAndEdgeRange(list);

        //    Assert.True(graph.Vertices.Count() == 22);
        //    Assert.True(graph.Edges.Count() == 21);
        //    Assert.True(subgraph.Vertices.Count() == 11);
        //    Assert.True(subgraph.Edges.Count() == 5);
        //    Assert.True(subgraph2.Vertices.Count() == 11);
        //    Assert.True(subgraph2.Edges.Count() == 6);

        //    Assert.True(graph.Vertices.First(x => x.Id == 126).Testdata == "126");
        //    Assert.True(subgraph.Vertices.First(x => x.Id == 126).Testdata == "126");
        //    Assert.True(subgraph2.Vertices.Count(x => x.Id == 126) == 0);

        //    Assert.True(graph.Vertices.First(x => x.Id == 12).Testdata == "12");
        //    Assert.True(subgraph.Vertices.First(x => x.Id == 12).Testdata == "12");
        //    Assert.True(subgraph2.Vertices.Count(x => x.Id == 12) == 0);
        //}
    }
}
