using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using AccessControlGraph;
using QuickGraph;
using VMware.Vim;

namespace ConsoleApplication13
{
    public class VMWareNode : NodeBase
    {
        public readonly string Id;

        public readonly string Type;

        public int ?SecLevel;

        /// <summary>
        /// Предполагаем что меток не будет больше 32 штук. Если будет, то надо это менять на BitArray. Но будет медленнее, печалька.
        /// </summary>
        public BitVector32 ?Labels;

        public VMWareNode(ManagedObjectReference moref)
        {
            Id = moref.Value;
            Type = moref.Type;
        }
        public override bool Equals(object obj)
        {
            return Id == ((VMWareNode) obj).Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public static class extensions
    {
        private static IEnumerable<VMWareNode> GetNodes(params ManagedObjectReference[][] morefs)
        {
            for(var i=0 ; i < morefs.Length ; i++)
                for (var j = 0; j < morefs[i].Length; j++)
                    if ( morefs[i][j]!=null ) 
                        yield return new VMWareNode(morefs[i][j]);
        }

        private static IEnumerable<VMWareNode> GetRefNodes(this Datacenter dc)
        {
            return GetNodes(dc.Datastore, new []{ dc.DatastoreFolder}, new []{ dc.HostFolder }, dc.Network, new []{ dc.NetworkFolder }, new []{ dc.VmFolder});
        }
        private static IEnumerable<VMWareNode> GetRefNodes(this ComputeResource cr)
        {
            return GetNodes(cr.Datastore, cr.Host, cr.Network, new []{ cr.ResourcePool });
        }
        private static IEnumerable<VMWareNode> GetRefNodes(this VirtualMachine vm)
        {
            return GetNodes(vm.Datastore, vm.Network, new []{ vm.ParentVApp }, new []{ vm.ResourcePool });
        }
        private static IEnumerable<VMWareNode> GetRefNodes(this ResourcePool rp)
        {
            return GetNodes(rp.resourcePool);
        }
        private static IEnumerable<VMWareNode> GetRefNodes(this Datastore ds)
        {
            return GetNodes(ds.Vm);
        }
        public static IEnumerable<VMWareNode> GetRefNodes(this EntityViewBase moref)
        {
            //TODO: убрать работу через dynamic(МЕДЛЕННО ЖЕЖ)
            dynamic a = moref;
            return GetRefNodes(a);
        }
    }
    class Program
    {
        private static string
            serviceUrl = @"https://10.72.14.25:80/sdk",
            username = @"administrator@vsphere.local",
            password = @"Gazprom*123";

        static void Main(string[] args)
        {
            Console.WriteLine("TICKS:");
            var sw = new Stopwatch();
            var graph = new AccessControlGraphRoot<VMWareNode>();
            var client = new VimClientImpl();
            client.Login(serviceUrl, username, password);
            
            //Добавляем обьекты в граф, а также отношения между ними.
            {
                Action<Type> fetch = type =>
                {
                    sw.Start();
                    var vms = client.FindEntityViews(type, client.ServiceContent.RootFolder, null, null);
                    
                    vms.ForEach(evb =>
                    {
                        var node = new VMWareNode(evb.MoRef) {Labels = new BitVector32(16), SecLevel = 0};
                        var edges = evb.GetRefNodes().ToList().ConvertAll(x => new QuickGraph.Edge<VMWareNode>(node, x));
                        graph.AddVerticesAndEdgeRange(edges);
                    });
                    sw.Stop();
                };
                new List<Type>
                {
                    typeof (Datacenter),
                    typeof (ComputeResource),
                    typeof (VirtualMachine),
                    typeof (ResourcePool),
                    typeof (Datastore)
                }.ForEach(fetch);
                Console.WriteLine("Added all nodes from VCenter to graph:" + sw.ElapsedMilliseconds);
            }

            //Выборка обьектов из графа, а также связанных с ними обьектов.
            {
                sw.Restart();
                for (int i = 0; i < 10000; i++)
                {
                    var res = graph.Vertices.Where(n => n.Type == "Datastore").ToList();
                    var resss = graph.AdjacentEdges(res[0]).ToList().ConvertAll(x => x.GetOtherVertex(res[0]));
                    res[0].SecLevel = 12;
                }
                sw.Stop();
                Console.WriteLine("10000 x Select VMWareNode's relevant Nodes:" + sw.ElapsedTicks);
            }

            //Выборка подграфа
            {
                VertexPredicate<VMWareNode> predicate = x => x.SecLevel == 12;
                sw.Restart();
                var subgraph = graph.GetChildGraph(predicate);
                sw.Stop();
                Console.WriteLine("Get Subtree:" + sw.ElapsedTicks);
                sw.Restart();
                var subgraphcached = graph.GetChildGraph(predicate);
                sw.Stop();
                Console.WriteLine("Get Subtree cached:" + sw.ElapsedTicks);
            }

            //Удаление обьекта из графа, а также всех его связей
            {
                sw.Restart();
                var res2 = graph.Vertices.Where(n => n.Type == "VirtualMachine").ToList();
                //graph.RemoveVertex(res2[0]);
                //graph.RemoveVertexIf(x => x.Labels.GetValueOrDefault().Data==16);
                sw.Stop();
                Console.WriteLine("Remove VMWareNode:" + sw.ElapsedTicks);
            }

            Console.ReadLine();
        }
    }
}
