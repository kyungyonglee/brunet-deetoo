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
  /**
   <summary>A hashtable contains the data for key:value pair.
   key = content, value = CacheEntry
   */
  public class CacheList : IEnumerable {
    //LiskedList<MemBlock> list_of_contents = new LinkedList<MemBlock>();
    protected Hashtable _data = new Hashtable();
    protected Node _node;
    protected RpcManager _rpc;
    /// <summary>number of string objects in the table.</summary>
    public int Count { get { return _data.Count; } }
    /// <summary>The node of this hashtable.</summary>
    public Address Owner { get { return _node.Address; } }
    public Hashtable Data { get { return _data; } }
    /*
     <summary>Create a new set of chached data(For now, data is strings).</summary>
     * 
     */
    public CacheList(Node node) { 
      _node = node;
      _rpc = RpcManager.GetInstance(node);
      ///add handler for deetoo data insertion and search.
      _rpc.AddHandler("Deetoo", new DeetooHandler(node,this));
    }
    /// <summary>need this for iteration</summary>
    public IEnumerator GetEnumerator() {
      IDictionaryEnumerator en = _data.GetEnumerator();
      while(en.MoveNext() ) {
        yield return en.Current;
      }
    }

    /*
     <summary></summay>Object insertion method.</summary>
     <param name="ce">A CacheEntry which is inserted to this node.</param>
     */
    public void Insert(CacheEntry ce) {
      //Console.WriteLine("Insertion is required here at {0}",_node.Address);
      string Key = ce.Content;
      if (!_data.ContainsKey(Key)  ) {
	//Console.WriteLine("~~~~~~~~~~~~Inserted!!!!!!!!!!");
        _data.Add(Key,ce);
      }
      else {
        //Console.WriteLine("An element with Key = {0} already exists.",Key);
      }
      //Console.WriteLine("_data.Count: {0}", _data.Count);
    }
    /**
     <summary>Overrided method for insertion, create new CacheEntry with inputs, then, insert CacheEntry to the CacheList.</summary>
     <param name="str">Content, this is key of hashtable.<param>
     <param name="alpha">replication factor.<param>
     <param name="a">start address of range.<param>
     <param name="b">end address of range.<param>
     */
    public void Insert(string str, double alpha, Address a, Address b) {
      //Console.WriteLine("*********************************");
      //Console.WriteLine("Insertion is required here at {0}",_node.Address);
      CacheEntry ce = new CacheEntry(str, alpha, a, b);
      if (!_data.ContainsKey(str)) { 
        _data.Add(str,ce);
	//Console.WriteLine("~~~~~~~~~~~~Inserted!!!!!!!!!!");
      }
      else {
        //Console.WriteLine("An element with Key = {0} already exists.",str);
      }
      //Console.WriteLine("_data.Count: {0}", _data.Count);
    }
    /**
     <summary>Regular Expression search method.</summary>
     <return>An array of matching strings from CacheList.</return>
     <param name = "pattern">A pattern of string one wish to find.</param>
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
    /**
     <summary>Perform an exact match.</summary>
     <return>A matching string in the CacheList.</return>
     <param name = "key">A string one wish to find.<param>
     <remarks>returns null if no matching string with a given key.</remarks>
     */
    public string ExactMatch(string key) {
      string result = null;
      if (_data.ContainsKey(key) )
      {
        result = key;
      }
      return result;
    } 
     /**
      <summary>Recalculate and replace range info for each CacheEntry. 
      Insert objects from nearest neighbors or remove objects based on 
      recalculated bounded broadcasting range.</summary>
     */
    public void Stabilize() {
      //Console.WriteLine("In CacheList, ------{0}",_node.Address); 
      foreach (DictionaryEntry dic in _data) {
        string this_key = (string)dic.Key;
	CacheEntry ce = (CacheEntry)dic.Value;
	/// Recalculate size of range.
	BigInteger rg_size = ce.GetRangeSize(_node);
        //Console.WriteLine("{0} key's new range size: {1}",this_key, rg_size);
	/// reassign range info based on recalculated range.
	ce.ReAssignRange(rg_size);
	//_data.Remove(this_key);
	//Console.WriteLine("start: {0}, end: {1}, alpha: {2}",ce.Start,ce.End, ce.Alpha);
	if (!ce.InRange(_node.Address)) {
	  //This node is not in this entry's range. 
	  //Remove this entry.
          //Console.WriteLine("not mine any more");
	  _data.Remove(this_key);
	}
      } 
    }
  }

}
