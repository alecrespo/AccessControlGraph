﻿using System;
using System.Linq;
using NUnit.Framework;
using QuickGraph;

namespace AccessControlGraph.Tests
{
    [TestFixture]
    public class AccessControlGraph
    {
        private AccessControlGraphRoot<TestNode> graph;
            
        [SetUp]
        public void SetUp()
        {
            //init graph with tree-like structure, so it will be easy to test operations on this graph.
            graph = new AccessControlGraphRoot<TestNode>();
            //Adding to node subnodes from start to start+count;
            
            Action<int, int, int> add = (node, start, count) =>
            {
                var n = new TestNode(node);
                graph.AddVerticesAndEdgeRange(
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
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;
        }
        [Test]
        public void SubgraphCreation()
        {
            //trying various graph filtering scheme
            var subgraph = graph.GetChildGraph(v => v.Id > 11);
            Assert.True(subgraph.Vertices.Count() == 19);
            Assert.True(subgraph.Edges.Count() == 12);

            var subgraph2 = graph.GetChildGraph(v => v.Id % 2 == 0);
            Assert.True(subgraph2.Vertices.Count() == 10);
            Assert.True(subgraph2.Edges.Count() == 4);

            var subgraph3 = graph.GetChildGraph(v => v.Id % 2 == 1);
            Assert.True(subgraph3.Vertices.Count() == 11);
            Assert.True(subgraph3.Edges.Count() == 6);
        }

        [Test]
        public void GraphUsingSameNodeObjects()
        {
            //change data before subgraph creation
            graph.Vertices.First(x => x.Id == 12).Testdata = "12";
            
            var subgraph = graph.GetChildGraph(v => v.Id % 2 == 0);

            //change data after subgraph creation 
            graph.Vertices.First(x => x.Id == 122).Testdata = "122";
            
            //make requests for subgraph nodes data;
            Assert.True(subgraph.Vertices.First(x => x.Id == 12).Testdata == "12");
            Assert.True(subgraph.Vertices.First(x => x.Id == 122).Testdata == "122");
        }

        [Test]
        public void CheckCache()
        {
            VertexPredicate<TestNode> testFunc = v => v.Id % 2 == 0;
            var subgraph = graph.GetChildGraph(testFunc);

            var cachedsubgraph = graph.GetChildGraph(testFunc);
            Assert.True(subgraph.Equals(cachedsubgraph));

            var uncachedsubgraph = graph.GetChildGraph(v => v.Id % 2 == 0); //uncached due to anonimous function != testFunc
            Assert.False(subgraph.Equals(uncachedsubgraph));
        }
    }
}
