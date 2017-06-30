var casper = require('casper').create();
var util = require('utils');
var userName=casper.cli.args[0];
var site='http://music.163.com'
var defaultheader={
	'User-Agent': 'Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36',
	'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
	'Accept-Encoding': 'gzip, deflate, sdch',
	'Accept-Language': 'zh-CN,zh;q=0.8',
	'Host' : 'music.163.com'
};

casper.then(function{casper.echo('StartFetching')});

casper.start('www.weibo.com',{
	header : defaultheader
});

casper.then(function{casper.echo('StartFetching')});

casper.waitForSelector('div.gn_search_v2 input');
casper.then(function(){
	this.click('div.gn_search_v2 input',"80%","50%");
	casper.capture('ww1.png');
});
casper.waitForSelector('ul.selectbox ul.selectbox');
casper.then(function(){
	this.click('ul.selectbox ul.selectbox a',"80%","50%");
	casper.capture('ww2.png');
});

casper.waitForSelector('a.W_texta.W_fb');
casper.then(function(){
	casper.capture('ww3.png');
})

casper.start();