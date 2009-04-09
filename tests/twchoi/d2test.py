#!/usr/bin/env python
from pybru import *
import string, time
import sys, xmlrpclib, random,math,base64


def choice():
  """return one letter or digit randomly"""
  s = string.letters + string.digits
  length = len(s)
  r = random.sample(s,1)
  return r[0]

def RStringGenerator(length=10):
  """ramdom string generator"""
  return ''.join([choice() for i in range(length)])

def getRange(net_size, alpha):
  """returns start and end of range given replication factor"""
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

#def node_choice(getNodes()):
#  return nodes[random.randrange(len(nodes))]

#def getNodes():
#  plab_rpc = xmlrpclib.ServerProxy('https://www.planet-lab.org/PLCAPI/',allow_none=True)
#  nodes = []
#  for node in plab_rpc.GetNodes({'AuthMethod': "anonymous"}, {}, ['hostname']):
#    nodes.append(node)
#  return nodes

alpha = (float)(sys.argv[1])

URL = "http://planetlab2.ece.ucdavis.edu:10000/xm.rem"
rpc = xmlrpclib.Server(URL)
net_size = rpc.localproxy("mapreduce.NetSize")
print net_size

cht = {}
input_list = []
cache_result = []
cht["task_name"]="Brunet.Deetoo.MapReduceCache"
print 'cache-----------'
print 'netsize	count	depth'
for i in xrange(10):
  rg_start, rg_end = getRange(net_size, alpha)
  #print rg_start, rg_end
  input = RStringGenerator()
  input_list.append(input)
  cht["gen_arg"]=[rg_start,rg_end]
  cht["map_arg"]=[input,alpha,rg_start,rg_end]
  time.sleep(6)
  result = rpc.localproxy("mapreduce.Start",cht)
  count = result['count']
  depth = result['height']
  print net_size, '\t',count, '\t', depth
  #cache_result.append(result)
#print "caching results"
#print cache_result
#End of Caching

#start querying
URL = "http://127.0.0.1:20000/queryxm.rem"
rpc = xmlrpclib.Server(URL)
net_size = rpc.localproxy("mapreduce.NetSize")
qht = {}
query_result = []
hit = 0
print 'query---------'
print 'object	size	hit	count	depth'
for q in input_list:
  rg_start, rg_end = getRange(net_size, alpha)
  #q_type = "exact"
  q_type = "regex"
  qht["task_name"]="Brunet.Deetoo.MapReduceQuery"
  qht["gen_arg"]=[rg_start,rg_end]
  qht["map_arg"]=[q,q_type]
  qht["reduce_arg"]=q_type
  time.sleep(5)
  result = rpc.localproxy("mapreduce.Start",qht)
  count = result['count']
  depth = result['height']
  if len(result['query_result']) != 0:
    hit = 1
  print q, '\t', net_size, '\t', hit, '\t', count, '\t', depth
  #query_result.append(result)
#print "querying result"
#print query_result
