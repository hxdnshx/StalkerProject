var casper = require('casper').create();
var util = require('utils');
var fs=require('fs');
var userName=casper.cli.args[0];
var site='http://music.163.com'
var defaultheader={
	'User-Agent': 'Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36',
	'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
	'Accept-Encoding': 'gzip, deflate, sdch',
	'Accept-Language': 'zh-CN,zh;q=0.8',
	'Host' : 'weibo.com'
};

//casper.then(function(){casper.echo('StartFetching');});

casper.start('http://www.weibo.com',{
	header : defaultheader
});

casper.viewport(1366,768);

casper.then(function(){
	casper.echo('StartFetching');
	casper.capture('ww1.png');
});

casper.waitForSelector('input.W_input',null,function(){
	casper.capture('ww2.png');
	fs.write('debug.html',casper.getHTML());
},10000);
casper.then(function(){
	this.echo('Input Found!')
	this.click('input.W_input',"80%","50%");
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

casper.run(function(){
	casper.exit();
});