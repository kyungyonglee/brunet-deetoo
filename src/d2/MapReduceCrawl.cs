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
using System.Collections.Specialized;

namespace Brunet.Deetoo {
  /**
   * This class implements a map-reduce task that allows Deetoo 
   * object caching.
   * BoundedBroadcast is used for tree-generation.
   */ 
  public class MapReduceCrawl: MapReduceBoundedBroadcast {
    //list of Deetoo object in a node, 
    //[content, start_range, end_range, replication_factor]
    //private Node _node;
    /*
     * @param cl CacheList object for caching.
     */
    public MapReduceCrawl(Node n): base(n) {
      //_node = n;
    }
    /*
     * Map method to add CachEntry to CacheList
     * @param map_arg [content,alpha,start,end]
     */
    public override object Map(object map_arg) {
      //IDictionary my_entry = new ListDictionary();
      //my_entry["count"]=1;
      int count = 1;
      return count;
    }
    
    /**
     * Reduce method
     * @param reduce_arg argument for reduce
     * @param current_result result of current map 
     * @param child_rpc results from children 
     * @param done if done is true, stop reducing and return result
     * return table of hop count, number of success, tree depth
     */
    public override object Reduce(object reduce_arg, 
                                  object current_result, RpcResult child_rpc,
                                  out bool done) {

      done = false;
      object child_result = child_rpc.Result;
      if (current_result == null) {
        return child_result;
      }
      int my_count = (int)(current_result);

      int child_count = (int)(child_result);
      int result = my_count + child_count;
      return result;
    }
  }
}
