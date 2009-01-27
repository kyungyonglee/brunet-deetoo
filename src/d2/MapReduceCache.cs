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

namespace Brunet.Deetoo {
  /**
   * This class implements a map-reduce task that allows regular 
   * expression search using Bounded Broadcasting. 
   */ 
  public class MapReduceCache: MapReduceBoundedBroadcast {
    private CacheList _cl;
    public MapReduceCache(Node n, CacheList cl): base(n) {
      _cl = cl;
    }
    public override object Map(object map_arg) {
      ArrayList arg = map_arg as ArrayList;	    
      CacheEntry ce = (CacheEntry)arg[0];
      int result = 0;   // if caching is successful, result will set to 1.
      int previous_count = _cl.Count;
      try {
        _cl.Insert(ce);
      }
      catch {

      }
      if (_cl.Count > previous_count) { result = 1; }
      IDictionary my_entry = new ListDictionary();
      my_entry["count"]=1;
      my_entry["height"]=1;
      my_entry["success"]=result;
      return my_entry;
    }
    
    public override object Reduce(object reduce_arg, 
                                  object current_result, RpcResult child_rpc,
                                  ref bool done) {

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
      my_entry["success"] = (int) my_entry["success"] + (int) value["success"];
      int y = (int) value["count"];
      my_entry["count"] = count + y;
      int z = (int) value["height"] + 1;
      if (z > max_height) {
        my_entry["height"] = z; 
      }
      return my_entry;
    }
  }
}
