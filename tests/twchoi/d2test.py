#!/usr/bin/env python
from pybru import *
import string, time
import sys, random,math,base64
import timeout_xmlrpclib as xmlrpclib

usage = """usage: python d2test.py installed_nodes_file_name alpha query_type """
def main():
  """ Default starting point"""
  try:
    file_name = sys.argv[1] # This file contains list of hostnames which have deetoonode installed
    alpha = (float)(sys.argv[2]) #replication factor, determines bounded broadcasting range
    query_type = sys.argv[3] #'exact' for exact match or 'regex' for regular expression search
  except:
    print usage
    return
  print 'caching start'
  input_objects = cacheAction(file_name, alpha)
  #time.sleep(1)  #time interval between caching and querying
  print 'start query'
  queryAction(input_objects, file_name, alpha, query_type)

def char_choice():
  """return one letter or digit randomly"""
  s = string.letters + string.digits
  length = len(s)
  r = random.sample(s,1)
  return r[0]

def RStringGenerator(length=10):
  """ramdom string generator"""
  return ''.join([char_choice() for i in range(length)])

def getRange(net_size, alpha):
  """returns start and end of range given replication factor"""
  # size of bounded broadcasting range (=\sqrt(\alpha / net_size) * addr_bin_size)
  rg = (int)(math.sqrt(alpha / (float)(net_size)) * 2**160)
  start_addr = 1
  while start_addr %2 != 0:
    start_addr = random.randint(0, 2**160-1)
  end_addr = start_addr + rg
  start = Address(start_addr)
  end = Address(end_addr)
  rg_start = start.str
  rg_end = end.str
  return rg_start, rg_end

def getRandomNode(file_name, query=False):
  """ return random node's url from the list of 
  deetoo installed plab nodes.
  file_name is list of nodes' hostname which installed deetoo
  if query is False, url is for caching node,
  otherwise, url is for querying node.
  """
  url = 0
  net_size = 0
  node_file = open(file_name,'r')
  nodes = [nd.split()[0] for nd in node_file] # hostnames
  max_net_size = len(nodes)
  port = 9845  #port number for cache node
  svc = "cache"  #name of service 
  if query:
    port = 9846  # port number for query node
    svc = "query" # name of service
  #while (url==0 or net_size==0):
  while (net_size <= 0):
    selected_node = nodes[random.randrange(len(nodes))] # select one live node from the list 
    url = "http://" + selected_node + ":" + str(port) + "/" + svc + "xm.rem"
    print url
    try:
      rpc = xmlrpclib.Server(url) # ser xmlrpc server
      net_size = rpc.localproxy("mapreduce.NetSize")  # estimated network size (StructuredNode.GetSize)
      #print 'in try: net_size: ', net_size
    except:
      net_size = 0
      #print 'in except: net_size: ', net_size
      continue
  return rpc, net_size, max_net_size

def cacheAction(c_in_file, alpha):
  """ insert 100 random string(lenth=10) into a random node.
      time interval between insertions is 600 sec(10 min)."""
  print '----------caching----------------'
  c_res_file = open("c_res.dat",'w') #caching output file
  c_ht = {} # hashtable for cache
  input_list = [] # list of inserted objects, will be passed to queryAction
  #cache_result = []
  c_ht["task_name"]="Brunet.Deetoo.MapReduceCache"
  print '#time		object	max_size	guesssize	count	depth	response_time\n'
  c_res_file.write('#time		object		max_n	g_size	count	depth	response_time\n')
  for i in xrange(100):
    #time.sleep(600)
    #print 'new object is about to be inserted'
    rpc, guess_size, max_size = getRandomNode(c_in_file)
    rg_start, rg_end = getRange(guess_size, alpha) #randomly selected range
    input = RStringGenerator() #input object
    c_ht["gen_arg"]=[rg_start,rg_end]
    c_ht["map_arg"]=[input,alpha,rg_start,rg_end]
    #c_ht["mstime"] = 300
    #time.sleep(60)
    print c_ht
    b_time = time.time()  #current time at caching started
    try: # see if mapreduce is timeout, if so, it returns nothing
      result = rpc.localproxy("mapreduce.Start",c_ht) #result returns hop_count and tree depth
    except:
      print "timed out"
      c_res_file.write("#timed out\n")
      continue
    a_time = time.time()  #current time at caching finished
    res_time = a_time - b_time  #response time
    #print res_time
    count = result['count']
    depth = result['height']
    input_list.append(input)
    print b_time, '\t', input, '\t',max_size, '\t', guess_size, '\t',count, '\t', depth, '\t', res_time
    out_str = str(b_time) + '\t' + input + '\t' + str(max_size) + '\t' + str(guess_size) +'\t' + str(count) + '\t' + str(depth) + '\t' + str(res_time) +'\n'
    c_res_file.write(out_str)
  c_res_file.close()
  return input_list

def queryAction(input_list, q_in_file, alpha, q_type):
  """querying
  send queries for inserted string objects 100 times each
  """
  print '---------------querying----------------'
  qht = {} #hashtable for query, input argument of MapRedeceQuery
  q_out_file = open("q_res.dat", 'w') # query output file
  #query_result = []
  qht["task_name"]="Brunet.Deetoo.MapReduceQuery"
  print 'time		object	max_size	guess_size	hit	count	depth	response_time\n'
  q_out_file.write('#time		object		max_n	g_size	hit	count	depth	response_time\n')
  for q in input_list:
    for it in xrange(100):
      rpc, guess_size, max_size = getRandomNode(q_in_file, True)
      rg_start, rg_end = getRange(guess_size, alpha)
      qht["gen_arg"]=[rg_start,rg_end]
      qht["map_arg"]=[q,q_type]
      qht["reduce_arg"]=q_type
      #qht["mstime"] = 300
      #time.sleep(1800)     #give 30 minutes of suspension between queries
      b_time = time.time()
      try:
        result = rpc.localproxy("mapreduce.Start",qht)
      except:
	print "timeout"
	q_out_file.write("#timed out\n")
	continue
      a_time = time.time()
      response_time = a_time - b_time
      #print response_time
      count = result['count']
      depth = result['height']
      q_result = result['query_result']
      print 'query_result', q_result
      hit = 0
      if (q_type == 'exact'):
        if q_result == q:
          hit = 1
      elif q_type == 'regex':
        if len(q_result) != 0:
          hit = 1
      else:
        print "no matching search option for ", q_type
	return
      print b_time, '\t', q, '\t', max_size, '\t', guess_size, '\t', hit, '\t', count, '\t', depth, '\t', response_time
      out_str = str(b_time) + '\t' + q + '\t' + str(max_size) + '\t' + str(guess_size) + '\t' + str(hit) + '\t' + str(count) + '\t' + str(depth) + '\t' + str(response_time) + '\n'
      q_out_file.write(out_str)
  q_out_file.close()

if __name__ == "__main__":
  main()
