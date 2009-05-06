/*
Copyright (C) 2008  David Wolinsky <davidiw@ufl.edu>, University of Florida

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.IO;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;
using System.Net;

using Brunet.DistributedServices;
//using Brunet.Deetoo;
using Brunet.Rpc;
using Brunet;
using Brunet.Deetoo;
using Brunet.Coordinate;

/**
\namespace Brunet::Applications Provides DeetooNode and core features
necessary for running Brunet.Node in a simple complete manner.
Besides providing basic functionality, this namespace and class offer 
some extra features, such as the ability to cleanly shutdown via ctrl-c,
a user configured Rpc method for providing information during a crawl
(see Information), and the ability to determine the IP addresses on the
local machines and their associated names.
\brief Provides DeetooNode which implements a simple Brunet P2P Node.
*/
namespace Brunet.Applications {
  /**
  <summary>DeetooNode provides the core Brunet features in a deployable model
  the inputs are a xml config file called NodeConfig, which specifies which
  if any services to deploy.  Other projects should inherit this as their
  base class rather than implementing their own interfaces to Brunet.</summary>
  */
  public class DeetooNode {
    /// <summary>The path to the NodeConfig.</summary>
    protected String _c_path;
    protected String _q_path;
    /// <summary>The NodeConfig that defines the Brunet.Node.</summary>
    protected NodeConfig _c_node_config;
    protected NodeConfig _q_node_config;
    /// <summary>The Brunet.Node used to connect to the p2p network.</summary>
    protected StructuredNode _c_node;
    protected StructuredNode _q_node;
    /// <summary>The Dht object used to participate in the dht.</summary>
    protected Dht _c_dht;
    protected Dht _q_dht;
    /// <summary>The NCService object used for this node.</summary>
    protected NCService _c_ncservice;
    protected NCService _q_ncservice;
    /// <summary>The DhtRpc service provider.</summary>
    protected DhtServer _c_ds;
    protected DhtServer _q_ds;
    /// <summary>The XmlRpc service provider.</summary>
    protected XmlRpcManagerServer _c_xrm;
    protected XmlRpcManagerServer _q_xrm;
    /// <summary>The shutdown service provider.</summary>
    protected Shutdown _shutdown;
    /// <summary>Path to the node config (for updating it).</summary>
    protected string _c_node_config_path;
    protected string _q_node_config_path;
    /**  <summary>True if the node should reincarnate itself if Node.Connect
    exits or throws an exception</summary>*/
    protected bool _running;
    protected CacheList _cs;

    /**
    <summary>Loads a configuration file and creates a Node.Address if
    necessary.</summary>
    <param name="path">The path to a NodeConfig</param>
    <returns>Exits if NodeConfig is invalid.</returns>
    */
    public DeetooNode(String c_path, String q_path) {
      try {
        _c_node_config = Utils.ReadConfig<NodeConfig>(c_path);
        _q_node_config = Utils.ReadConfig<NodeConfig>(q_path);
      }
      catch {
        Console.WriteLine("Invalid or missing configuration file.");
        Environment.Exit(1);
      }

      _c_node_config_path = c_path;
      _q_node_config_path = q_path;
      if(_c_node_config.NodeAddress == null) {
	AHAddress c_addr = Utils.GenerateAHAddress();
        _c_node_config.NodeAddress = c_addr.ToString();
	_q_node_config.NodeAddress = AddressTranspose(c_addr).ToString();
        Utils.WriteConfig(c_path, _c_node_config);
	Utils.WriteConfig(q_path, _q_node_config);
      }
      _running = true;
      //_rpc.AddHandler("Deetoo", new DeetooHandler(node,this));
    }

    /**
    <summary>A constructor to be used only by sub-classes.  The goal here being
    that inheritors may want to implement their own subclass of config but
    DeetooNode still needs to be configured and possibly write to the config
    file.  This gets around that problem!</summary>
    <param name="path">The Path of the NodeConfig in the second parameter
    </param>
    <param name="config">A NodeConfig inherited object.</param>
    protected DeetooNode(String path, NodeConfig config) {
      _node_config = config;
      if(_node_config.NodeAddress == null) {
        _node_config.NodeAddress = (Utils.GenerateAHAddress()).ToString();
        Utils.WriteConfig(path, _node_config);
      }
      _running = true;
    }
    */

    /**
    <summary>This should be called by the Main after all the setup is done
    this passes control to the _node and won't return until the program is
    exiting.  (It is synchronous.)</summary>
    */
    public virtual void Run() {
      int sleep = 60, sleep_min = 60, sleep_max = 3600;
      DateTime start_time = DateTime.UtcNow;
      // Keep creating new nodes no matter what!
      while(_running) {
        CreateNode("cache");
        CreateNode("query");
        new Information(_c_node, "CacheNode");
        new Information(_q_node, "QueryNode");
        Console.Error.WriteLine("CacheNode is connected to {0} as {1}.  Current time is {2}.",
                                _c_node.Realm, _c_node.Address.ToString(), DateTime.UtcNow);
        Console.Error.WriteLine("QueryNode is connected to {0} as {1}.  Current time is {2}.",
                                _q_node.Realm, _q_node.Address.ToString(), DateTime.UtcNow);
        _c_node.DisconnectOnOverload = true;
        _q_node.DisconnectOnOverload = true;
        start_time = DateTime.UtcNow;
        StartServices();
	Thread c_thread = new Thread(_c_node.Connect );
	c_thread.Start();
	_q_node.Connect();
	c_thread.Join();
	/*
	Thread q_thread = new Thread(_q_node.Connect );
	q_thread.Start();
        //_q_node.Connect();
	_c_node.Connect();
	q_thread.Join();
	*/
        SuspendServices();
        if(!_running) {
          break;
        }
        // Assist in garbage collection
        DateTime now = DateTime.UtcNow;
        Console.Error.WriteLine("Going to sleep for {0} seconds. Current time is: {1}", sleep, now);
        Thread.Sleep(sleep * 1000);
        if(now - start_time < TimeSpan.FromSeconds(sleep_max)) {
          sleep *= 2;
          sleep = (sleep > sleep_max) ? sleep_max : sleep;
        }
        else {
          sleep /= 2;
          sleep = (sleep < sleep_min) ? sleep_min : sleep;
        }
      }
    }

    /**
    <summary>Creates a Brunet.Node, the resulting node will be available in
    the class as _node.</summary>
    <remarks>The steps to creating a node are first constructing it with a
    namespace, optionally adding local ip addresses to bind to, specifying
    local end points, specifying remote end points, and finally registering
    the dht.</remarks>
    */
    public virtual void CreateNode(string type) {
      NodeConfig node_config = null;
      StructuredNode current_node = null;
      AHAddress address = null;
      if (type == "cache") {
        node_config = _c_node_config;
        address = (AHAddress) AddressParser.Parse(node_config.NodeAddress);
        current_node = new StructuredNode(address, node_config.BrunetNamespace);
      }
      else if ( type == "query" ) {
        node_config = _q_node_config;
        address = (AHAddress) AddressParser.Parse(node_config.NodeAddress);
        current_node = new StructuredNode(address, node_config.BrunetNamespace);
      }
      else {
        throw new Exception("Unrecognized node type: " + type);
      }
      IEnumerable addresses = IPAddresses.GetIPAddresses(node_config.DevicesToBind);

      Brunet.EdgeListener el = null;
      foreach(NodeConfig.EdgeListener item in node_config.EdgeListeners) {
        int port = item.port;
        if (item.type =="tcp") {
          try {
            el = new TcpEdgeListener(port, addresses);
          }
          catch {
            el = new TcpEdgeListener(0, addresses);
          }
        }
        else if (item.type == "udp") {
          try {
            el = new UdpEdgeListener(port, addresses);
          }
          catch {
            el = new UdpEdgeListener(0, addresses);
          }
        }
        else {
          throw new Exception("Unrecognized transport: " + item.type);
        }
	current_node.AddEdgeListener(el);
      }
      el = new TunnelEdgeListener(current_node);
      current_node.AddEdgeListener(el);

      ArrayList RemoteTAs = null;
      if(node_config.RemoteTAs != null) {
        RemoteTAs = new ArrayList();
        foreach(String ta in node_config.RemoteTAs) {
          RemoteTAs.Add(TransportAddressFactory.CreateInstance(ta));
        }
	current_node.RemoteTAs = RemoteTAs;
      }

      try {
        if (node_config.NCService.Enabled) {
	  if (type == "cache") {
            _c_ncservice = new NCService(current_node, node_config.NCService.Checkpoint);
            if (node_config.NCService.OptimizeShortcuts) {
              current_node.Sco.TargetSelector = new VivaldiTargetSelector(current_node, _c_ncservice);
	    }
	  }
	  else {
            _q_ncservice = new NCService(current_node, node_config.NCService.Checkpoint);
            if (node_config.NCService.OptimizeShortcuts) {
              current_node.Sco.TargetSelector = new VivaldiTargetSelector(current_node, _q_ncservice);
	    }
	  }

        }
      } catch {}

      if (type == "cache") {
        _c_dht = new Dht(current_node, 3, 20);
        _cs = new CacheList(current_node);
        current_node.MapReduce.SubscribeTask(new MapReduceCache(current_node,_cs));
        current_node.MapReduce.SubscribeTask(new MapReduceCrawl(current_node));
	_c_node = current_node;
      }
      else {
        _q_dht = new Dht(current_node, 3, 20);
	CacheList q_cs = new CacheList(current_node);
        current_node.MapReduce.SubscribeTask(new MapReduceQuery(current_node,_cs));
        current_node.MapReduce.SubscribeTask(new MapReduceCrawl(current_node));
	_q_node = current_node;
      }

    }

    /**
    <summary>Starts services such as shutdown, rpcdht, and xmlrpc.  If you wish
    to have your own shutdown path, edit OnExit instead of this.  This can be
    called multiple times without negative effect.</summary>
    */
    public virtual void StartServices() {
      _shutdown = Shutdown.GetShutdown();
      if(_shutdown != null) {
        _shutdown.OnExit += OnExit;
      }
      
      if(_c_node_config.RpcDht != null && _c_node_config.RpcDht.Enabled) {
        if(_c_ds == null) {
          _c_ds = new DhtServer(_c_node_config.RpcDht.Port);
        }
        _c_ds.Update(_c_dht,"cache");
      }
      if(_q_node_config.RpcDht != null && _q_node_config.RpcDht.Enabled) {
        if(_q_ds == null) {
          _q_ds = new DhtServer(_q_node_config.RpcDht.Port);
        }
        _q_ds.Update(_q_dht,"query");
      }

      if(_c_node_config.XmlRpcManager != null && _c_node_config.XmlRpcManager.Enabled) {
        if(_c_xrm == null) {
          _c_xrm = new XmlRpcManagerServer(_c_node_config.XmlRpcManager.Port);
        }
        _c_xrm.Update(_c_node,"cache");
      }
      if(_q_node_config.XmlRpcManager != null && _q_node_config.XmlRpcManager.Enabled) {
        if(_q_xrm == null) {
          _q_xrm = new XmlRpcManagerServer(_q_node_config.XmlRpcManager.Port);
        }
        _q_xrm.Update(_q_node,"query");
      }

    }

    /**
    <summary>If you no longer want to serve due to inactivity or for whatever
    reason, this will be active until StartServices is called again.  This just
    stops serving the DhtRpc and XmlRpc services, but the ports remain open.
    </summary>
     */
    public virtual void SuspendServices() {
      if(_c_ds != null) {
        _c_ds.Stop();
      }
      if(_c_xrm != null) {
        _c_xrm.Suspend();
      }
      if(_q_ds != null) {
        _q_ds.Stop();
      }
      if(_q_xrm != null) {
        _q_xrm.Suspend();
      }
    }

    /**
    <summary>This stops all services such as Xml and DhtRpc. Call this instead
    of SuspendServices if the node is shutting down or services are no longer 
    required and you would like to release the ports</summary>
    */
    public virtual void StopServices() {
      if(_c_ds != null) {
        _c_ds.Stop();
        _c_ds = null;
      }
      if(_c_xrm != null) {
        _c_xrm.Stop();
        _c_xrm = null;
      }
      if(_q_ds != null) {
        _q_ds.Stop();
        _q_ds = null;
      }
      if(_q_xrm != null) {
        _q_xrm.Stop();
        _q_xrm = null;
      }
    }

    /**
    <summary>This method is registered as a delegate to Shutdown.OnExit and
    will be called when ctrl-c is pressed by the user.  This stops services,
    prevents the node from reincarnating, and then disconnects the node.
    </summary>
    */
    public virtual void OnExit() {
      if(_c_ncservice != null && _c_node_config.NCService.Checkpointing) {
        string checkpoint = _c_ncservice.GetCheckpoint();
        string prev_cp = _c_node_config.NCService.Checkpoint;
        string empty_cp = (new Point()).ToString();
        if(!checkpoint.Equals(prev_cp) && !checkpoint.Equals(empty_cp)) {
          _c_node_config.NCService.Checkpoint = checkpoint;
          Utils.WriteConfig(_c_node_config_path, _c_node_config);
        }
      }
      if(_q_ncservice != null && _c_node_config.NCService.Checkpointing) {
        string checkpoint = _q_ncservice.GetCheckpoint();
        string prev_cp = _c_node_config.NCService.Checkpoint;
        string empty_cp = (new Point()).ToString();
        if(!checkpoint.Equals(prev_cp) && !checkpoint.Equals(empty_cp)) {
          _c_node_config.NCService.Checkpoint = checkpoint;
          Utils.WriteConfig(_c_node_config_path, _q_node_config);
        }
      }

      StopServices();
      _running = false;
      _c_node.Disconnect();
      _q_node.Disconnect();
    }
    /**
    <summary>This method returns matching querying address from caching address.<\summary>
    */
    protected AHAddress AddressTranspose(AHAddress a) {
      BigInteger one = new BigInteger(1);
      BigInteger s_BIN = one << 80;
      BigInteger addr = a.ToBigInteger();
      BigInteger addr_j = addr % s_BIN;
      BigInteger addr_i = (addr - addr_j) / s_BIN;
      BigInteger q = addr_j * s_BIN + addr_i;
      //AHAddress q_addr = new AHAddress(q);
      byte[] target = Address.ConvertToAddressBuffer(q);
      //Address.SetClass(target, _c_node.Address.Class);
      Address.SetClass(target, a.Class);
      AHAddress q_addr = new AHAddress(target);
      return q_addr;
    }
    /**
    <summary>Runs the DeetooNode.  This should be implemented in all inherited
    classes.</summary>
    <remarks>
    <para>To execute this at a command-line using Mono:</para>
    <code>
    mono DeetooNode.exe path/to/node_config
    </code>
    <para>To execute this at a command-line using Windows .NET:</para>
    <code>
    DeetooNode.exe path/to/node_config
    </code>
    </remarks>
    <param name="args">The command line argument required is a path to a
    NodeConfig</param>
    */
    public static int Main(String[] args) {
      DeetooNode node = new DeetooNode(args[0],args[1]);
      node.Run();
      return 0;
    }

  }
}
