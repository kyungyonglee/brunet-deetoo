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
    //protected int _count = 0;
    public int Count { get { return _data.Count; } }
    public Address Owner { get { return _node.Address; } }
    public Hashtable Data { get { return _data; } }
    public CacheList(Node node) { 
      _node = node;
    }
    /**
    public CacheList() { 

    }

    public CacheList(CacheEntry ce) {
      string Key = ce.Content;
      _data.Add(Key,ce);
    }
    */
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
      CacheEntry ce = new CacheEntry(str, alpha, a, b);
      if (!_data.ContainsKey(str)) { 
        _data.Add(str,ce);
      }
      else {
        //Console.WriteLine("An element with Key = {0} already exists.",str);
      }
      //Console.WriteLine("_data.Count: {0}", _data.Count);
    }
    /*
     * Regular Expression search method
     * return matching string list from CacheList
     * @pattern candidate regular expression
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
    public string ExactMatch(string pattern) {
      string result = null;
      if (_data.ContainsKey(pattern) )
      {
        result = pattern;
      }
      return result;
    } 
    /*
     * ask neighbors to push their CacheList 
     */
    /*
     * Upon requesting update, a remote node push 
     * its CacheList to a requesting node.
     */
    /*
    public ArrayList Push(ArrayList keys) {
      ArrayList result = new ArrayList();
      Console.WriteLine("-----start push at {0}",_node.Address);
      if (_data.Count != 0) {
        foreach(DictionaryEntry de in _data) {
	  string this_key = (string)de.Key;
	  CacheEntry ce = (CacheEntry)de.Value;
	  CacheEntry new_ce = ReCalculateRange(ce);
	  _data.Remove(this_key);
	  _data.Add(this_key,new_ce);
          if (!keys.Contains(this_key) ) {
            Console.WriteLine("Send {0} to the requesting node.",this_key);
            result.Add(ce);
	  }
        }
      }
      return result;
    }
    */
    public void Stabilize() {
      
      foreach (DictionaryEntry dic in _data) {
        string this_key = (string)dic.Key;
	CacheEntry ce = (CacheEntry)dic.Value;
	BigInteger rg_size = ce.GetRangeSize(_node);
	ce.ReCalculateRange(rg_size);
	_data.Remove(this_key);
	_data.Add(this_key,ce);
      } 
    }
  }

}
