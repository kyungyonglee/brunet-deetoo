

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Brunet.Deetoo
{
  public class CacheEntry {
    protected Hashtable _ht = new Hashtable(4);
    public static explicit operator Hashtable(CacheEntry c) {
      return c._ht;
    }
    public string Content {
      get { return (string) _ht["content"]; }
      set { _ht["content"] = value; }
    }
    public float Alpha {
      get { return (float) _ht["alpha"]; }
      set { _ht["alpha"] = value; }
    }
    public Address Start {
      get { return (AHAddress) _ht["start"]; }
      set { _ht["start"] = value; }
    }
    public Address End {
      get { return (AHAddress) _ht["end"]; }
      set { _ht["end"] = value; }
    }
    public CacheEntry(string content, float alpha, Address start, Address end) {
      this.Content = content;
      this.Alpha = alpha;
      this.Start = start;
      this.End = end;
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
    /*
    public bool Match(string pattern) {
      if (Count == 0) {
        Regex match_pattern = new Regex(pattern);
        IDictionaryEnumerator en = list_of_contents;
    }
    */
    public ArrayList RegExMatch(string pattern) { 
      ArrayList result = null;
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
      string result = null;
      foreach(CacheEntry de in _data)
      {
        if (de.Content == pattern) {
	  result = de.Content;	
	}
	
      }
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
