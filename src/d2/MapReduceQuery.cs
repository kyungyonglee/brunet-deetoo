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
using System.Collections.Specialized;
using System.Text.RegularExpressions;

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
    /*
     * Map method to add CachEntry to CacheList
     * @param map_arg [pattern, query_type]
     */
    public override object Map(object map_arg) {
      ArrayList map_args = map_arg as ArrayList;
      string pattern = (string)(map_args[0]);
      string query_type = (string)(map_args[1]);
      //Console.WriteLine("query_type:{0}", query_type);
      IDictionary my_entry = new ListDictionary();
      if (query_type == "regex") {
        ArrayList query_result = (ArrayList)(_cl.RegExMatch(pattern));
	my_entry["query_result"] = query_result;
      }
      else if (query_type == "exact") {
        string exact_result = _cl.ExactMatch(pattern);
        my_entry["query_result"] = exact_result;
      }
      else {
        throw new AdrException(-32608, "No Deetoo match option with this name: " +  query_type);
      }
      my_entry["count"] = 1;
      my_entry["height"] = 1;
      //Console.WriteLine("in map: map_result.count: {0}", my_entry.Count);
      return my_entry;
    }
      
    /**
     * Reduce method
     * @param reduce_arg argument for reduce
     * @param current_result result of current map 
     * @param child_rpc results from children 
     * @param done if done is true, stop reducing and return result
     * return table of hop count, tree depth, and query result
     */  
    public override object Reduce(object reduce_arg, 
                                  object current_result, RpcResult child_rpc,
                                  out bool done) {

      done = false;
      //ISender child_sender = child_rpc.ResultSender;
      string query_type = (string)reduce_arg;
      object child_result = null;
      try {
        child_result = child_rpc.Result;
      }
      catch {
        throw new Exception("no rpc result.");
      }
      //child result is a valid result
      if (current_result == null) {
        return child_result;
      }
      IDictionary my_entry = current_result as IDictionary;
      IDictionary value = child_result as IDictionary;
      int max_height = (int) (my_entry["height"]);
      int count = (int) (my_entry["count"]);
      int y = (int) value["count"];
      my_entry["count"] = count + y;
      int z = (int) value["height"] + 1;
      if (z > max_height) {
        my_entry["height"] = z; 
      }
      if (query_type == "exact") {
        string m_result = (string)(my_entry["query_result"]); //current result
	string c_result = (string)(value["query_result"]); //child result
	if (m_result != null) {
          //Console.WriteLine("m_result is not null");
	  // if query type is exact matching and current result is not an empty string, 
	  // stop searching and return the result immediately.
	  done = true;
	}
	else {
	  if (c_result != null) {
            //Console.WriteLine("c_result is not null");
            done = true;
	    my_entry["query_result"] = c_result;
	  }
	  else {
            //there is no valid result, return null for the entry
            my_entry["query_result"] = null;
	  }
	}
	return my_entry;
      }
      else if (query_type == "regex") {
        ArrayList q_result = (ArrayList)(my_entry["query_result"]);
        ArrayList c_result = (ArrayList)(value["query_result"]);
        q_result.AddRange(c_result); //concatenate current result with child result
        my_entry["query_result"] = q_result;
        return my_entry;
      }
      else {
        throw new AdrException(-32608, "This query type {0} is supported." + query_type);
      }
    }
  }
}
