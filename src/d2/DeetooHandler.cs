/*
This program is part of BruNet, a library for the creation of efficient overlay
networks.
Copyright (C) 2008 Taewoong Choi <twchoi@ufl.edu> University of Florida  

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
using System.Collections;
using System.Collections.Generic;

/**
\namespace Brunet::Deetoo
\brief Provides Deetoo caching and querying services using the Brunet P2P infrastructure
 */
namespace Brunet.Deetoo
{
  /**
   * <summary>This class provides a client interface to CacheList class.<\summary>
   */
  public class DeetooHandler : IRpcHandler  {
    /// <summary>The node to provide services for.<\summary>
    protected Node _node;
    /// <summary>The RpcManager to perform data transfer.<\summary>
    protected readonly RpcManager _rpc;
    /// <summary>The collection of cached data.(Hashtable)<\summary>
    protected CacheList _cl;
    /// <summary> The left neighbor of this node.<\summary>
    protected Address _left_addr = null;
    /// <summary> The right neighbor of this node.<\summary>
    protected Address _right_addr = null;
    /// <summary>The total amount of cached data.</summary>
    public int Count { get { return _cl.Count; } }
    /**
    <summary>This provides translation for Rpc methods.<\summary> 
    <param name="caller">The ISender who made the request.<\param>
    <param name="method">The method requested.</param>
    <param name="args">A list of arguments to pass to the method.</param>
    <param name="req_state">The return state sent back to the RpcManager so that it
    knows who to return the result to.</param>
    <exception>Thrown when there the method is not InsertHandler</exception>
    <remark>This handler is registered in CacheList constructor.<\remark>
    */
    public void HandleRpc(ISender caller, string method, IList args, object req_state) {
      object result = null;
      try {
        if (method == "InsertHandler") {
	  string content = (string)args[0];
	  double alpha = (double)args[1];
	  AHAddress start = (AHAddress)AddressParser.Parse((string)args[2]);
	  AHAddress end = (AHAddress)AddressParser.Parse((string)args[3]);
	  CacheEntry ce = new CacheEntry(content, alpha, start, end);
          result = InsertHandler(ce);
	}
	else if (method == "count") {
          result = _cl.Count;
	}
	else {
          throw new Exception("DeetooHandler.Exception: No Handler for method: " + method);
	}
      }
      catch (Exception e) {
        result = new AdrException(-32602, e);
      }
      _rpc.SendResult(req_state,result);
    }
    /**
    <summary>This handles only connection event for now.<\summary>
    <param name="node">The node the deetoo is to serve from.<\param>
    <param name="cl">The CacheList belongs to this node.<\param>
    */
    public DeetooHandler(Node node, CacheList cl) {
      _node = node;
      _rpc = RpcManager.GetInstance(node);
      _cl = cl;
      node.ConnectionTable.ConnectionEvent += this.ConnectionHandler;
      //_rpc.AddHandler("Deetoo", this);
    }
    /**
     <summary>This is called when ConnectionEvent occurs.
     First, recalculate ranges of entries(stabilize).
     Then, if target node is in the object's range,
     call InsertHandler on the remote node which 
     insert entry to the remote node's CacheList.<\summary>
     <param name="con">The Connection which copies CacheEntries from this node.<\param>
     */
    public void Put(Connection con) {
      //Console.WriteLine("Put is called here at {0}",_node.Address);
      //Console.WriteLine("data size: {0}",_cl.Count);
      if( _cl.Count > 0 ) {
	//Console.WriteLine("start stabilization");
	// Before data are transferred, recalculate each object's range
	// If the node is out of new range, entry will be removed from local list.
        _cl.Stabilize();
	//Console.WriteLine("-+-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
	//Console.WriteLine("_cl.Count: {0}", _cl.Count);
        foreach(DictionaryEntry de in _cl) {
	  CacheEntry ce = (CacheEntry)de.Value;
	  //Console.WriteLine("in Put in foreach this entry : {0}", ce.Content);
          Channel queue = new Channel(1);
	  queue.CloseAfterEnqueue();
	  /*
	  queue.CloseEvent += delegate(object o, EventArgs args) {
	    Channel q = (Channel)o;
	    if (q.Count != 0) {
	      RpcResult rres = (RpcResult)queue.Dequeue();
	      bool res = false;
	      try {
                res = (bool)rres.Result;
	      }
	      catch (Exception e) {
                Console.WriteLine("{0} Exception caught. Insertion failed.",e);
	      }
	      //_rpc.SendResult(req_state,res);
	    }
	  };
	  */
	  //Console.WriteLine("=================before invoke");
          AHAddress addr = (AHAddress)con.Address;
	  //Console.WriteLine("new connection: {0}",addr);
          if (ce.InRange(addr) ) {
	    //If new connection is within the range, ask to insert this object.
	    //Console.WriteLine("in range. add it");
	    try {
              _rpc.Invoke(con.Edge,queue,"Deetoo.InsertHandler",ce.Content, ce.Alpha, ce.Start.ToString(), ce.End.ToString());
	    }
	    catch (Exception e){
	      Console.WriteLine("{0} Exception caught.",e);
	    }
	  }
	}
      }
    }
    /**
     <summary><\summary>
     <param name="o">The CacheEntry about to be inserted<\param>
     */
    public bool InsertHandler(object o) {
      //Console.WriteLine("------------------------------------");
      //Console.WriteLine("Inserthandler is called here");
      CacheEntry ce = (CacheEntry)o;
      //Console.WriteLine("before: {0}", _cl.Count);
      bool result = false;
      try {
        _cl.Insert(ce);
	result = true;
      }
      catch {
        throw new Exception("ENTRY_ALREADY_EXISTS");
      }
      //Console.WriteLine("after: {0}", _cl.Count);
      //Console.WriteLine("Cache success?????????????? {0}",result);
      return result;
    }
    /**
    <summary>This is called whenever there is a disconnect or a connect, the
    idea is to determine if there is a new left or right node, if there is and
    here is a pre-existing transfer, we must interupt it, and start a new
    transfer.</summary>
    <remarks>The possible scenarios where this would be active:
     - no change on left
     - new left node with no previous node (from disc or new node)
     - left disconnect and new left ready
     - left disconnect and no one ready
     - no change on right
     - new right node with no previous node (from disc or new node)
     - right disconnect and new right ready
     - right disconnect and no one ready
    </remarks>
    <param name="o">Unimportant</param>
    <param name="eargs">Contains the ConnectionEventArgs, which lets us know
    if this was a Structured Connection change and if it is, we should check
    the state of the system to see if we have a new left or right neighbor.
    </param>
    */
    protected void ConnectionHandler(object o, EventArgs eargs) {
      ConnectionEventArgs cargs = eargs as ConnectionEventArgs;
      //Console.WriteLine("ConnectionHandler is called here at {0}",_node.Address);
      Connection old_con = cargs.Connection;
      ConnectionTable tab = _node.ConnectionTable;
      Connection lc = null, rc = null;
      try {
        lc = tab.GetLeftStructuredNeighborOf((AHAddress) _node.Address);
    }
      catch(Exception) {}
      try {
        rc = tab.GetRightStructuredNeighborOf((AHAddress) _node.Address);
      }
      catch(Exception) {}
      if(lc != null) {
        if(lc.Address != _left_addr) {
          _left_addr = lc.Address;
        }
        if(Count > 0) {
	  //Console.WriteLine("PUT+++++++ lc addr: {0}",_left_addr);
          Put(lc); 
        }
      }
      //Console.WriteLine("1_right_addr: {0}", _right_addr); 
      if(rc != null) {
        if(rc.Address != _right_addr) {
          //Console.WriteLine("right connection is different");
          _right_addr = rc.Address;
        }
        if(Count > 0) {
	  //Console.WriteLine("PUT++++   rc addr: {0}",_right_addr);
          Put(rc); 
        }
      }
      //Console.WriteLine("2rc: {0}", rc.Address); 
    }
  }
}
