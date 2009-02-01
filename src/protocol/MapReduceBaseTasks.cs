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
  /**
  public abstract class MapReduceBoundedBroadcast: MapReduceTask 
  {
    public MapReduceBoundedBroadcast(Node n):base(n) {}
    public override MapReduceInfo[] GenerateTree(MapReduceArgs mr_args) 
    {
      object gen_arg = mr_args.GenArg;
      string end_range = gen_arg as string;
      Log("generating child tree, range end: {0}.", end_range);
      AHAddress end_addr = (AHAddress) AddressParser.Parse(end_range);
      AHAddress start_addr = _node.Address as AHAddress;
      //we are at the start node, here we go:
      ConnectionTable tab = _node.ConnectionTable;
      ConnectionList structs = tab.GetConnections(ConnectionType.Structured);
      ArrayList retval = new ArrayList();

      if (structs.Count > 0) {
        Connection curr_con = structs.GetLeftNeighborOf(_node.Address);
        int curr_idx = structs.IndexOf(curr_con.Address);
        //keep going until we leave the range
        int count = 0;
        ArrayList con_list = new ArrayList();
        while (count++ < structs.Count && ((AHAddress) curr_con.Address).IsBetweenFromLeft(start_addr, end_addr)) {
          con_list.Add(curr_con);
          //Log("adding connection: {0} to list.", curr_con.Address);
          curr_idx  = (curr_idx + 1)%structs.Count;
          curr_con = structs[curr_idx];
        }
        
        Log("{0}: {1}, number of child connections: {2}", 
            this.TaskName, _node.Address, con_list.Count);
        for (int i = 0; i < con_list.Count; i++) {
          MapReduceInfo mr_info = null;
          ISender sender = null;
          Connection con = (Connection) con_list[i];
          sender = (ISender) con.Edge;
          //check if last connection
          if (i == con_list.Count - 1) {
            mr_info = new MapReduceInfo( (ISender) sender, 
                                         new MapReduceArgs(this.TaskName, 
                                                           mr_args.MapArg, //map argument
                                                           end_range, //generate argument
                                                           mr_args.ReduceArg //reduce argument
                                                           ));
            
            Log("{0}: {1}, adding address: {2} to sender list, range end: {3}", 
                this.TaskName, _node.Address, 
                con.Address, end_range);
            retval.Add(mr_info);
          }
          else {
            string child_end = ((Connection) con_list[i+1]).Address.ToString();
            mr_info = new MapReduceInfo( sender,
                                         new MapReduceArgs(this.TaskName,
                                                           mr_args.MapArg, 
                                                           child_end,
                                                           mr_args.ReduceArg));
            Log("{0}: {1}, adding address: {2} to sender list, range end: {3}", 
                this.TaskName, _node.Address, 
                con.Address, child_end);
            retval.Add(mr_info);
          }
        }
      }
      return (MapReduceInfo[]) retval.ToArray(typeof(MapReduceInfo));
    }
  } 
*/  
    /** The following class provides the base class for tasks utilizing the
   *  BoundedBroadcastTree generation.
   */
  public abstract class MapReduceBoundedBroadcast: MapReduceTask 
  {
    public MapReduceBoundedBroadcast(Node n):base(n) {}
    /**
     * Generates tree for bounded broadcast. Algorithm works as follows:
     * The goal is to broadcast to all nodes in range [local_address, end).
     * Given a range [local_address, b), determine all connections that belong to this range.
     * Let the connections be b_1, b_2, ..... b_n.
     * To connection bi assign the range [b_i, b_{i+1}).
     * To the connection bn assign range [b_n, end).]
     */
    public override MapReduceInfo[] GenerateTree(MapReduceArgs mr_args) 
    {
      object gen = mr_args.GenArg;
      ArrayList gen_list = gen as ArrayList;
      string start_range = gen_list[0] as string;
      string end_range = gen_list[1] as string;
      foreach(string rg_arg in gen_list) {
       Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
       Console.WriteLine("range info: {0} ",rg_arg);
      }
      bool in_range;
      ArrayList map_arg = new ArrayList();
      ArrayList gen_arg = new ArrayList();
      ArrayList red_arg = new ArrayList();
      map_arg.Add(mr_args.MapArg);
      red_arg.Add(mr_args.ReduceArg);
      AHAddress start_addr = (AHAddress) AddressParser.Parse(start_range);
      AHAddress end_addr = (AHAddress) AddressParser.Parse(end_range);
      AHAddress this_addr = _node.Address as AHAddress;
      Console.WriteLine("------------------------------------this_addr: {0}, start: {1}, end: {2}", this_addr, start_range, end_range);
      Log("generating child tree, range start: {0}, range end: {1}.", start_range, end_range);
      //we are at the start node, here we go:
      ConnectionTable tab = _node.ConnectionTable;
      ConnectionList structs = tab.GetConnections(ConnectionType.Structured);
      ArrayList retval = new ArrayList();
      MapReduceInfo mr_info = null;

      if (this_addr.IsBetweenFromLeft(start_addr, end_addr) ) {
	      //this node is within the given range (start_addr, end_addr)
	in_range = true;
	map_arg.Add(in_range);
	red_arg.Add(in_range);
        ArrayList left_cons = new ArrayList();
        ArrayList right_cons = new ArrayList();
	if (structs.Count > 0) {
	  foreach(Connection c in structs) {
            AHAddress adr = (AHAddress)c.Address;
	    if(adr.IsBetweenFromLeft(this_addr, end_addr) ) {
              left_cons.Add(c);
	    }
	    else if (adr.IsBetweenFromLeft(start_addr, this_addr) ) {
              right_cons.Add(c);
	    }
	    else {
              //Out of Range. Do nothing!
	    }
	  }
	  //Make a compare and add it to ConnectionTable to sort by Address
          ConnectionLeftComparer left_cmp = new ConnectionLeftComparer(this_addr);
	  left_cons.Sort(left_cmp);
          ConnectionLeftComparer right_cmp = new ConnectionLeftComparer(this_addr);
	  right_cons.Sort(right_cmp);
	  //Print out neighbour list after sorting.
	  if (left_cons.Count != 0) {
	    PrintConnectionList(left_cons);
	  }
	  Console.WriteLine("-------------------------");
	  if (right_cons.Count !=0) {
	    PrintConnectionList(right_cons);
	  }

	  AHAddress last = this_addr;
	  //navigate left connections first
          for (int i = 0; i < left_cons.Count; i++) {
	    //MapReduceInfo mr_info = null;
	    ISender sender = null;
	    Connection next_c = (Connection)left_cons[i];
	    AHAddress next_addr = (AHAddress)next_c.Address;
	    sender = (ISender) next_c.Edge;
	    string front = last.ToString();
	    string back = next_addr.ToString();
	    gen_arg.Clear();
	    if (i==left_cons.Count -1) {
	      // The last bit
	      gen_arg.Add(front);
	      gen_arg.Add(end_range);
	      //Console.WriteLine("*********************************************gen_args Count: {0}",gen_arg.Count);
	      //Console.WriteLine("next: {0}, gen[0]: {1}, gen[1]: {2} ", next_c.Address, gen_arg[0], gen_arg[1]);
	      mr_info = new MapReduceInfo( (ISender) sender,
			                   new MapReduceArgs(this.TaskName,
						             map_arg,
							     gen_arg,
							     red_arg));
	      /*
	      mr_info = new MapReduceInfo( (ISender) sender, 
			                 new MapReduceArgs(this.taskName,
				                           mr_args.MapArg,
							   in_range,
					                   front,
					                   end_range,
					                   mr_args.ReduceArg));
	      */
	      Log("{0}: {1}, adding address: {2} to sender list, range start: {3}, range end: {4}",
			    this.TaskName, _node.Address, next_c.Address,
			    front, end_range);
	      //Console.WriteLine("++left  this_addr: {0}, next: {1}, start: {2}, end: {3}",
		//	    _node.Address, next_c.Address, front, end_range);
	      retval.Add(mr_info);
	      Console.WriteLine("************************* retval.Count: {0}", retval.Count);
	      foreach (MapReduceInfo Mri in retval) {
		MapReduceArgs Mrargs = Mri.ARGS as MapReduceArgs;
		ArrayList gen_arg1 = Mrargs.GenArg as ArrayList;
	        Console.WriteLine("########### add retval here: gen_arg[0]: {0}, gen_arg{1}: ", gen_arg1[0], gen_arg1[1] );
	      }
	    }
	    else {
	      gen_arg.Add(front);
	      gen_arg.Add(back);
	      //Console.WriteLine("*********************************************gen_args Count: {0}",gen_arg.Count);
	      //Console.WriteLine("next: {0}, gen[0]: {1}, gen[1]: {2} ", next_c.Address, gen_arg[0], gen_arg[1]);
              mr_info = new MapReduceInfo( (ISender) sender, 
			                   new MapReduceArgs(this.TaskName,
						             map_arg,
							     gen_arg,
							     red_arg));
	      Log("{0}: {1}, adding address: {2} to sender list, range start: {3}, range end: {4}",
			    this.TaskName, _node.Address, next_c.Address,
			    front, back);
	      //Console.WriteLine("++left(last)  this_addr: {0}, next: {1}, start: {2}, end: {3}",
		//	    _node.Address, next_c.Address, front, back);
	      retval.Add(mr_info);
	      Console.WriteLine("************************* retval.Count: {0}", retval.Count);
	      foreach (MapReduceInfo Mri in retval) {
		MapReduceArgs Mrargs = Mri.ARGS as MapReduceArgs;
		ArrayList gen_arg1 = Mrargs.GenArg as ArrayList;
	        Console.WriteLine("########### add retval here: gen_arg[0]: {0}, gen_arg{1}: ", gen_arg1[0], gen_arg1[1] );
	      }
	    }
	    last = next_addr;
	    // check if list is sorted or not
	    foreach(string stri in gen_arg) {
	      Console.WriteLine("gen_args: {0} ", stri);
	    }
	  }
	  //move to the right connections.
	  last = this_addr;
          for (int i = 0; i < right_cons.Count; i++) {
	    //MapReduceInfo mr_info = null;
	    ISender sender = null;
	    Connection next_c = (Connection)right_cons[i];
	    AHAddress next_addr = (AHAddress)next_c.Address;
	    sender = (ISender) next_c.Edge;
	    string front = next_addr.ToString();
	    string back = last.ToString();
	    gen_arg.Clear();
	    if (i==right_cons.Count -1) {
	      gen_arg.Add(start_range);
	      //gen_arg.Add(front);
	      gen_arg.Add(back);
	      //Console.WriteLine("*********************************************");
	      //Console.WriteLine("*********************************************gen_args Count: {0}",gen_arg.Count);
	      //Console.WriteLine("next: {0}, gen[0]: {1}, gen[1]: {2} ", next_c.Address, gen_arg[0], gen_arg[1]);
	      mr_info = new MapReduceInfo( (ISender) sender, 
			                 new MapReduceArgs(this.TaskName,
				                           map_arg, 
					                   gen_arg,
					                   red_arg));
	      Log("{0}: {1}, adding address: {2} to sender list, range start: {3}, range end: {4}",
			    this.TaskName, _node.Address, next_c.Address,
			    start_range, front);
	      //Console.WriteLine("++right  this_addr: {0}, next: {1}, start: {2}, end: {3}",
	      //		    _node.Address, next_c.Address, start_range, back);
	      retval.Add(mr_info);
	      Console.WriteLine("************************* retval.Count: {0}", retval.Count);
	      foreach (MapReduceInfo Mri in retval) {
		MapReduceArgs Mrargs = Mri.ARGS as MapReduceArgs;
		ArrayList gen_arg1 = Mrargs.GenArg as ArrayList;
	        Console.WriteLine("########### add retval here: gen_arg[0]: {0}, gen_arg{1}: ", gen_arg1[0], gen_arg1[1] );
	      }
	    }
	    else {
	      gen_arg.Add(front);
	      gen_arg.Add(back);
	      //Console.WriteLine("*********************************************");
	      //Console.WriteLine("*********************************************gen_args Count: {0}",gen_arg.Count);
	      //Console.WriteLine("next: {0}, gen[0]: {1}, gen[1]: {2} ", next_c.Address, gen_arg[0], gen_arg[1]);
              mr_info = new MapReduceInfo( (ISender) sender, 
			                   new MapReduceArgs(this.TaskName,
				                           map_arg, 
					                   gen_arg,
					                   red_arg));
	      Log("{0}: {1}, adding address: {2} to sender list, range start: {3}, range end: {4}",
			    this.TaskName, _node.Address, next_c.Address,
			    back, front);
	      //Console.WriteLine("++right(last)  this_addr: {0}, next: {1}, start: {2}, end: {3}",
		//	    _node.Address, next_c.Address, front, back);
	      retval.Add(mr_info);
	      Console.WriteLine("************************* retval.Count: {0}", retval.Count);
	      foreach (MapReduceInfo Mri in retval) {
		MapReduceArgs Mrargs = Mri.ARGS as MapReduceArgs;
		ArrayList gen_arg1 = Mrargs.GenArg as ArrayList;
	        Console.WriteLine("########### add retval here: gen_arg[0]: {0}, gen_arg{1}: ", gen_arg1[0], gen_arg1[1] );
	      }
	    }
	    last = next_addr;
	    // check if list is sorted or not
	    foreach(string stri in gen_arg) {
	      Console.WriteLine("gen_args: {0} ", stri);
	    }
	  }
	}
	else {
          //this node is a leaf node.
	}
      }
      else { // _node is out of range. Just pass it to the closest to the middle of range.
	in_range = false;
	map_arg.Add(in_range);
	red_arg.Add(in_range);
        BigInteger up = start_addr.ToBigInteger();
	BigInteger down = end_addr.ToBigInteger();
	BigInteger mid_range = (up + down) /2;
	if (mid_range % 2 == 1) {mid_range = mid_range -1; }
	AHAddress mid_addr = new AHAddress(mid_range);
	if (!mid_addr.IsBetweenFromLeft(start_addr, end_addr) ) {
          mid_range += Address.Half;
	  mid_addr = new AHAddress(mid_range);
	}
	gen_arg.Clear();
	if (NextGreedyClosest(mid_addr) != null ) {
          AHGreedySender ags = new AHGreedySender(_node, mid_addr);
	  gen_arg.Add(start_range);
	  gen_arg.Add(end_range);
          mr_info = new MapReduceInfo( (ISender) ags,
			                new MapReduceArgs(this.TaskName,
						          map_arg,
							  gen_arg,
							  red_arg));
	  Log("{0}: {1}, out of range, moving to the closest node to mid_range: {2} to target node, range start: {3}, range end: {4}",
			  this.TaskName, _node.Address, mid_addr, start_addr, end_addr);
	  retval.Add(mr_info);
	}
	else  {
          // cannot find a node in the range. 
	}
      }
      Console.WriteLine("----- end of GenerateTree ----------");
      foreach(MapReduceInfo mri in retval) {
	MapReduceArgs mrargs = mri.ARGS as MapReduceArgs;
        ArrayList this_gen_arg = mrargs.GenArg as ArrayList;
	Console.WriteLine("######### in retval: gen_arg[0]: {0}, gen_arg{1}: ", this_gen_arg[0], this_gen_arg[1] );
	
      }
      return (MapReduceInfo[]) retval.ToArray(typeof(MapReduceInfo));
    }
    /**
    private bool InRange(AHAddress addr, AHAddress start, AHAddress end) {
      bool betw = addr.IsBetweenFromLeft(start, end);
      if (addr == start || addr == end) {
        return true;
      }
      else {
        return betw;
      }
    }
    */
        /*
     * @return closest neighbor to the target.
     * return null if there is no closer connection to the target.
     * @param a the target
     */

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
        /*
     * This is to see if connection list elements are sorted or not.
     */
    public void PrintConnectionList(ArrayList l) {
      for(int i = 0; i < l.Count; i++) {
	Connection c = (Connection)l[i];
	AHAddress next_add = (AHAddress)c.Address;
	AHAddress this_addr = (AHAddress)_node.Address;
	BigInteger dist = this_addr.LeftDistanceTo(next_add);
        Console.WriteLine("add: {0}, dis: {1}", next_add, dist);
      }
    }
  }
}
