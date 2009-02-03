

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Brunet.Deetoo
{
  public class CacheEntry {
    protected Hashtable _ht = new Hashtable(4);
    //public Node node = null;
    public static explicit operator Hashtable(CacheEntry c) {
      return c._ht;
    }
    public string Content {
      get { return (string) _ht["content"]; }
      set { _ht["content"] = value; }
    }
    public double Alpha {
      get { return (double) _ht["alpha"]; }
      set { _ht["alpha"] = value; }
    }
    public Address Start {
      get { return (Address) _ht["start"]; }
      set { _ht["start"] = value; }
    }
    public Address End {
      get { return (Address) _ht["end"]; }
      set { _ht["end"] = value; }
    }
    public CacheEntry(string content, double alpha, Address start, Address end) {
      this.Content = content;
      this.Alpha = alpha;
      this.Start = start;
      this.End = end;
    }
    public CacheEntry(Hashtable ht) {
      Content = (string)ht["content"];
      Alpha = (double)ht["alpha"];
      Start = (Address)ht["start"];
      End = (Address)ht["end"];
    }
    public bool Equal(CacheEntry ce) {
      if (this.Content == ce.Content) {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
  public class CacheList : IRpcHandler  {
    //LiskedList<MemBlock> list_of_contents = new LinkedList<MemBlock>();
    Hashtable _data = new Hashtable();
    protected Node _node;
    protected RpcManager _rpc;
    protected int _count = 0;
    public int Count { get { return _data.Count; } }
    public void HandleRpc(ISender caller, string method, IList args, object req_state) {
      object result = null;
      try {
        if (method == "insert") {
	  CacheEntry ce = (CacheEntry)args[0];
          Insert(ce);
        }
        else if (method == "exactmatch") {
	  string pattern = (string)args[0];
          result = ExactMatch(pattern);
        }
	else if (method == "regexmatch") {
          string pattern = (string)args[0];
	  result = RegExMatch(pattern);
	}
        else {
          throw new Exception("Cache.Exception: No Handler for method: " + method);
        }
      }
      catch (Exception e) {
        result = new AdrException(-32602, e);
      }
      _rpc.SendResult(req_state,result);
    }
    public CacheList(Node node) { 
      _node = node;
      _rpc = RpcManager.GetInstance(node);
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
      try { 
        _data.Add(Key,ce);
      }
      catch {
        Console.WriteLine("An element with Key = {0} already exists.",Key);
      }
    }
    public void Insert(string str, double alpha, Address a, Address b) {
      CacheEntry ce = new CacheEntry(str, alpha, a, b);
      //string Key = ce.Content;
      try { 
        _data.Add(str,ce);
      }
      catch {
        Console.WriteLine("An element with Key = {0} already exists.",str);
      }
    }
    /*
    public bool Match(string pattern) {
      if (Count == 0) {
        Regex match_pattern = new Regex(pattern);
        IDictionaryEnumerator en = list_of_contents;
    }
    */
    public List<string> RegExMatch(string pattern) { 
      List<string> result = new List<string>();
      Regex match_pattern = new Regex(pattern);
      //foreach(DictionaryEntry de in _data)
      foreach(CacheEntry de in _data)
      //IDictionaryEnumerator en = _data.GetEnumerator();
      //while (en.MoveNext() )
      {
	//CacheEntry entry = (CacheEntry) de;
	//string this_key = entry.Content;
	string this_key = de.Content;
        if (match_pattern.IsMatch(this_key)) {
	  result.Add(this_key);
	}
      }
      return result;
    }
    public string ExactMatch(string pattern) {
      Console.WriteLine("00_--------------");
      string result = null;
      Console.WriteLine("01_--------------");
      if (_data.ContainsKey(pattern) )
      {
	Console.WriteLine("1_--------------");
        result = pattern;
	Console.WriteLine("2_--------------");
      }
      Console.WriteLine("3_--------------");
      return result;
    } 
    /*
     * ask neighbors to push their CacheList 
     */
    public void Update() {
      //Node rht_nei = _node.  // right neighbor
      //Node lft_nei = _node.   // left neighbor
      //

    }
    /*
     * Upon requesting update, a remote node push 
     * its CacheList to a requesting node.
     */
    public void Push() {

    }
    public void Get() {

    }
  }
}
