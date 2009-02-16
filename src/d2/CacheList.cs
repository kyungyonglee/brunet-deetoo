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
using System.Text.RegularExpressions;
//using Brunet.Applications;

namespace Brunet.Deetoo
{
  public class CacheList  {
    //LiskedList<MemBlock> list_of_contents = new LinkedList<MemBlock>();
    protected Hashtable _data = new Hashtable();
    protected Node _node;
    protected RpcManager _rpc;
    //protected int _count = 0;
    public int Count { get { return _data.Count; } }
    public Address Owner { get { return _node.Address; } }
    public Hashtable Data { get { return _data; } }
    /*
     * CacheList is a set of chached data(For now, data is strings).
     * 
     */
    public CacheList(Node node) { 
      _node = node;
      _rpc = RpcManager.GetInstance(node);
      _rpc.AddHandler("Deetoo", new DeetooHandler(node,this));
    }
    public void Insert(CacheEntry ce) {
      string Key = ce.Content;
      if (!_data.ContainsKey(Key)  ) {
        _data.Add(Key,ce);
      }
      else {
        //Console.WriteLine("An element with Key = {0} already exists.",Key);
      }
      //Console.WriteLine("_data.Count: {0}", _data.Count);
    }
    public void Insert(string str, double alpha, Address a, Address b) {
      Console.WriteLine("*********************************");
      Console.WriteLine("Insertion is required here at {0}",_node.Address);
      CacheEntry ce = new CacheEntry(str, alpha, a, b);
      if (!_data.ContainsKey(str)) { 
        _data.Add(str,ce);
	Console.WriteLine("~~~~~~~~~~~~Inserted!!!!!!!!!!");
      }
      else {
        //Console.WriteLine("An element with Key = {0} already exists.",str);
      }
      //Console.WriteLine("_data.Count: {0}", _data.Count);
    }
    /*
     * Regular Expression search method
     * return list of matching strings from CacheList
     * @pattern regular expression
    */
    public ArrayList RegExMatch(string pattern) { 
      ArrayList result = new ArrayList();
      Regex match_pattern = new Regex(pattern);
      foreach(DictionaryEntry de in _data)
      {
	string this_key = (string)(de.Key);
        if (match_pattern.IsMatch(this_key)) {
	  result.Add(this_key);
	}
      }
      return result;
    }
    /*
     * ExactMatch returns matching string in the CacheList.
     * returns null if no matching string with a given key.
     */
    public string ExactMatch(string key) {
      string result = null;
      if (_data.ContainsKey(key) )
      {
        result = key;
      }
      return result;
    } 
     /*
     * Stabilize adjusts bounded broadcasting range based on 
     * current estimated network size.
     * recalculate range and replace range info with new range on each entry.
     */
    public void Stabilize() {
      Console.WriteLine("In CacheList, ------{0}",_node.Address); 
      foreach (DictionaryEntry dic in _data) {
        string this_key = (string)dic.Key;
	CacheEntry ce = (CacheEntry)dic.Value;
	BigInteger rg_size = ce.GetRangeSize(_node);
        Console.WriteLine("{0} key's new range size: {1}",this_key, rg_size);
	ce.ReCalculateRange(rg_size);
	_data.Remove(this_key);
	_data.Add(this_key,ce);
      } 
    }
  }

}
