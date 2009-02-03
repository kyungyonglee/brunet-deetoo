/*
This program is part of BruNet, a library for the creation of efficient overlay
networks.
Copyright (C) 2008  Arijit Ganguly <aganguly@gmail.com>, University of Florida
                    Taewoong Choi <twchoi1103@gmail.com>, University of Florida
                    P. Oscar Boykin <boykin@pobox.com>, University of Florida

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
//using System.Collections.Specialized;

/** Base map-reduce tasks. */
namespace Brunet {
  /** 
   * The following class provides a base class for tasks utilizing a greedy tree
   *  for computation. 
   */
  public abstract class MapReduceGreedy: MapReduceTask {
    public MapReduceGreedy(Node n):base(n) {}
    public override MapReduceInfo[] GenerateTree(MapReduceArgs mr_args) {
      object gen_arg = mr_args.GenArg;
      Log("{0}: {1}, greedy generator called, arg: {2}.", 
          this.TaskName, _node.Address, gen_arg);
      string address = gen_arg as string;
      AHAddress a =  (AHAddress) AddressParser.Parse(address);
      ArrayList retval = new ArrayList();
      ConnectionTable tab = _node.ConnectionTable;
      ConnectionList structs = tab.GetConnections(ConnectionType.Structured);
      Connection next_closest = structs.GetNearestTo((AHAddress) _node.Address, a);
      if (next_closest != null) {
        MapReduceInfo mr_info = new MapReduceInfo( (ISender) next_closest.Edge,
                                                   mr_args); //arguments do not change at all
        retval.Add(mr_info);
      }
      
      Log("{0}: {1}, greedy generator returning: {2} senders.", 
          this.TaskName, _node.Address, retval.Count);
      return (MapReduceInfo[]) retval.ToArray(typeof(MapReduceInfo));
    }
  }
  
  /** The following class provides the base class for tasks utilizing the
   *  BoundedBroadcastTree generation.
   */
  public abstract class MapReduceBoundedBroadcast: MapReduceTask 
  {
    public MapReduceBoundedBroadcast(Node n):base(n) {}
    /**
     * Generates tree for bounded broadcast. Algorithm works as follows:
     * The goal is to broadcast to all nodes in range (start, end).
     * Given a range (a, b), determine all connections that belong to this range.
     * Let the left connections be l_1, l_2, ..... l_n.
     * Let the right connections be r_1, r_2, ... , r_n.
     * To left connection l_i assign the range [b_{i-1}, b_i).
     * To right connection r_i assign the range [r_i, r_{i-1}]
     * To the connection ln assign range [l_{n-1}, end)
     * To the connection rn assign range (start, r_{n-1}]
     */
    public override MapReduceInfo[] GenerateTree(MapReduceArgs mr_args) 
    {
      ArrayList gen_list = mr_args.GenArg as ArrayList;
      string start_range = gen_list[0] as string;
      string end_range = gen_list[1] as string;
      AHAddress this_addr = _node.Address as AHAddress;
      AHAddress start_addr = (AHAddress) AddressParser.Parse(start_range);
      AHAddress end_addr = (AHAddress) AddressParser.Parse(end_range);
      Log("generating child tree, range start: {0}, range end: {1}.", start_range, end_range);
      //we are at the start node, here we go:
      ConnectionTable tab = _node.ConnectionTable;
      ConnectionList structs = tab.GetConnections(ConnectionType.Structured);
      ArrayList retval = new ArrayList();
      if (InRange(this_addr, start_addr, end_addr)) {
	if (structs.Count > 0) {
          //make connection list in the range.
          //left connection list is a list of neighbors which are in the range (this node, end of range)
          //right connection list is a list of neighbors which are in the range (start of range, this node)
          ArrayList cons = GetConnectionInfo(this_addr, start_addr, end_addr, structs);
          List<Connection> left_cons =  cons[0] as List<Connection>;
          List<Connection> right_cons = cons[1] as List<Connection>;
	  /*
	  //Print out neighbour list after sorting.
	  if (left_cons.Count != 0) {
	    PrintConnectionList(left_cons);
	  }
	  Console.WriteLine("-------------------------");
	  if (right_cons.Count !=0) {
	    PrintConnectionList(right_cons);
	  }
	  */
	  retval = GenerateTreeInRange(this_addr, start_addr, end_addr, left_cons, true, mr_args);
	  ArrayList ret_right = GenerateTreeInRange(this_addr, start_addr, end_addr, right_cons, false, mr_args);
	  retval.AddRange(ret_right);
	}
	else {  //this node is a leaf node.
          MapReduceInfo mr_info = null;
	  retval.Add(mr_info);
	  //Console.WriteLine("no connection in the range: return null info");
	}
	//Console.WriteLine("````````````retval.Count: {0}",retval.Count);
      }
      else { // _node is out of range. Just pass it to the closest to the middle of range.
        retval = GenerateTreeOutRange(start_addr, end_addr, mr_args);
        //Console.WriteLine("-----OUT RANGE++++++  ----------");
      }
      return (MapReduceInfo[]) retval.ToArray(typeof(MapReduceInfo));
    }

    private ArrayList GenerateTreeInRange(AHAddress this_addr, AHAddress start, AHAddress end, List<Connection> cons, bool left, MapReduceArgs mr_args) {
      //Divide the range and trigger bounded broadcasting again in divided range starting with neighbor.
      //Deivided ranges are (start, n_1), (n_1, n_2), ... , (n_m, end)
      ArrayList retval = new ArrayList();
      if (cons.Count != 0) //make sure if connection list is not empth!
      {
        //con_list is sorted.
	AHAddress last = this_addr;
	//the first element of cons is the nearest.
        for (int i = 0; i < cons.Count; i++) {
	  //MapReduceInfo mr_info = null;
	  Connection next_c = (Connection)cons[i];
	  AHAddress next_addr = (AHAddress)next_c.Address;
	  ISender sender = (ISender) next_c.Edge;
	  string front = last.ToString();
	  string back = next_addr.ToString();
	  string rg_start = start.ToString();
	  string rg_end = end.ToString();
          ArrayList gen_arg = new ArrayList();
	  if (i==cons.Count -1) {  // The last bit
            if (left) {
	      // the left farthest neighbor 
	      gen_arg.Add(front);
	      gen_arg.Add(rg_end);
	    }
	    else {
	      // the right farthest neighbor
              gen_arg.Add(rg_start);
	      gen_arg.Add(front);
	    }
	  }
	  else {
	    if (left) { //left connections
              gen_arg.Add(front);
	      gen_arg.Add(back);
	    }
	    else {  //right connections
	      gen_arg.Add(back);
              gen_arg.Add(front);
	    }

	  }
	  MapReduceInfo mr_info = new MapReduceInfo( (ISender) sender,
	 		                              new MapReduceArgs(this.TaskName,
					               	             mr_args.MapArg,
								     gen_arg,
								     mr_args.ReduceArg));
          Log("{0}: {1}, adding address: {2} to sender list, range start: {3}, range end: {4}",
				    this.TaskName, _node.Address, next_c.Address,
				    gen_arg[0], gen_arg[1]);
	  last = next_addr;
	  retval.Add(mr_info);
	}
      }
      return retval;
    }    
    private ArrayList GenerateTreeOutRange(AHAddress start, AHAddress end, MapReduceArgs mr_args) {
      ArrayList retval = new ArrayList();
      BigInteger up = start.ToBigInteger();
      BigInteger down = end.ToBigInteger();
      BigInteger mid_range = (up + down) /2;
      if (mid_range % 2 == 1) {mid_range = mid_range -1; }
	AHAddress mid_addr = new AHAddress(mid_range);
	if (!mid_addr.IsBetweenFromLeft(start, end) ) {
          mid_range += Address.Half;
	  mid_addr = new AHAddress(mid_range);
      }
      ArrayList gen_arg = new ArrayList();
      if (NextGreedyClosest(mid_addr) != null ) {
        AHGreedySender ags = new AHGreedySender(_node, mid_addr);
	string start_range = start.ToString();
	string end_range = end.ToString();
	gen_arg.Add(start_range);
	gen_arg.Add(end_range);
        MapReduceInfo mr_info = new MapReduceInfo( (ISender) ags,
				                new MapReduceArgs(this.TaskName,
							          mr_args.MapArg,
								  gen_arg,
								  mr_args.ReduceArg));
	Log("{0}: {1}, out of range, moving to the closest node to mid_range: {2} to target node, range start: {3}, range end: {4}",
			  this.TaskName, _node.Address, mid_addr, start, end);
	retval.Add(mr_info);
      }
      else  {
        // cannot find a node in the range. 
      }
      return retval;
    }


    protected Connection NextGreedyClosest(AHAddress a) {
    /*
     * First find the Connection pointing to the node closest to a, if
     * there is one closer than us
     */
      ConnectionTable tab = _node.ConnectionTable;
      ConnectionList structs = tab.GetConnections(ConnectionType.Structured);
    
      Connection next_closest = null;
      int idx = structs.IndexOf(a);
      if( idx < 0 ) {
        //a is not the table:
        Connection right = structs.GetRightNeighborOf(a);
        Connection left = structs.GetLeftNeighborOf(a);
        BigInteger my_dist = ((AHAddress)_node.Address).DistanceTo(a).abs();
        BigInteger ld = ((AHAddress)left.Address).DistanceTo(a).abs();
        BigInteger rd = ((AHAddress)right.Address).DistanceTo(a).abs();
        if( (ld < rd) && (ld < my_dist) ) {
          next_closest = left;
        }
        if( (rd < ld) && (rd < my_dist) ) {
          next_closest = right;
        }
      }
      else {
        next_closest = structs[idx];
      }    
      return next_closest;
    }
    private ArrayList GetConnectionInfo(AHAddress t_addr, AHAddress start, AHAddress end, ConnectionList cl) {
       
      //this node is within the given range (start_addr, end_addr)
      ArrayList ret = new ArrayList();
      List<Connection> left_con_list = new List<Connection>();
      List<Connection> right_con_list = new List<Connection>();
      foreach(Connection c in cl) {
        AHAddress adr = (AHAddress)c.Address;
        if(adr.IsBetweenFromLeft(t_addr, end) ) {
          left_con_list.Add(c);
        }
        else if (adr.IsBetweenFromLeft(start, t_addr) ) {
          right_con_list.Add(c);
        }
        else {
          //Out of Range. Do nothing!
        }
      }
      //Make a compare and add it to ConnectionTable to sort by Address
      ConnectionLeftComparer left_cmp = new ConnectionLeftComparer(t_addr);
      left_con_list.Sort(left_cmp);
      ConnectionRightComparer right_cmp = new ConnectionRightComparer(t_addr);
      right_con_list.Sort(right_cmp);
      ret.Add(left_con_list);
      ret.Add(right_con_list);
      return ret;
    }
  
    /*
     * This is to see if connection list elements are sorted or not.
     */
    private void PrintConnectionList(List<Connection> l) {
      for(int i = 0; i < l.Count; i++) {
	Connection c = (Connection)l[i];
	AHAddress next_add = (AHAddress)c.Address;
	AHAddress this_addr = (AHAddress)_node.Address;
	BigInteger dist = this_addr.LeftDistanceTo(next_add);
        Console.WriteLine("add: {0}, dis: {1}", next_add, dist);
      }
    }
    //returns true if addr is in a given range including boundary.
    /**
     * This returns true if addr is between start and end in a ring.
     * IsBetweenFrom*() excludes both start and end, but InRange() includes both.
     * @param addr, this node's address
     * @param start, the beginning address of range
     * @param end, the ending address of range
     */
    public bool InRange(AHAddress addr, AHAddress start, AHAddress end) {
      bool betw = addr.IsBetweenFromLeft(start,end);
      if (addr == start || addr == end) {return true;}
      else { return betw; }
    }
  }
}
