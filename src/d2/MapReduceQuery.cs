/*
This program is part of BruNet, a library for the creation of efficient overlay
networks.
Copyright (C) 2008 Taewoong Choi <twchoi@ufl.edu> University of Florida  
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
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Brunet;

namespace Brunet.Deetoo {
  /**
   * This class implements a map-reduce task that allows regular 
   * expression search using Bounded Broadcasting. 
   */ 
  public class MapReduceQuery: MapReduceBoundedBroadcast {
    private CacheList _cl;
    public MapReduceQuery(Node n, CacheList cl): base(n) {
      _cl = cl;
    }
    public override object Map(object map_arg) {
      //ArrayList cache = new ArrayList();
      ArrayList map_args = map_arg as ArrayList;
      string pattern = (string)map_args[0];
      string query_type = (string)map_args[1];
      ArrayList query_result = null;
      if (query_type == "regex") {
        query_result = _cl.RegExMatch(pattern);
      }
      else if (query_type == "exact") {
        string exact_result = _cl.ExactMatch(pattern);
	query_result.Add(exact_result);

      }
      IDictionary my_entry = new ListDictionary();
      my_entry["count"] = 1;
      my_entry["height"] = 1;
      my_entry["query_result"] = query_result;
      return my_entry;
    }
    
    public override object Reduce(object reduce_arg, 
                                  object current_result, RpcResult child_rpc,
                                  out bool done) {
      done = false;

      ISender child_sender = child_rpc.ResultSender;
      //the following can throw an exception, will be handled by the framework
      object child_result = child_rpc.Result;
      
      //child result is a valid result
      if (current_result == null) {
        return child_result;
      }
      
      IDictionary my_entry = current_result as IDictionary;
      IDictionary value = child_result as IDictionary;
      int max_height = (int) my_entry["height"];
      int count = (int) my_entry["count"];
      //int hits = (int) my_entry["hits"];
      ArrayList q_result = (ArrayList)my_entry["query_result"];
      ArrayList c_result = (ArrayList)value["query_result"];
      my_entry["query_result"] = q_result.Add(c_result);

      int y = (int) value["count"];
      my_entry["count"] = count + y;
      int z = (int) value["height"] + 1;
      if (z > max_height) {
        my_entry["height"] = z; 
      }
      //int x = (int) value["no_hit"] + hits;
      return my_entry;
    }
  }
}
