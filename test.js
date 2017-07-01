var casper = require('casper').create({
	pageSettings: {
        loadImages:  true,        // The WebPage instance used by Casper will
        loadPlugins: true         // use these settings
    }
});
var fs=require('fs');
var defaultheader={
	'User-Agent': 'User-Agent	Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)',
	'Accept': 'Accept	text/html, application/xhtml+xml, */*',
	'Accept-Encoding': 'gzip, deflate',
	'Accept-Language': 'zh-CN',
	'Host' : 'weibo.com'
};

var cookieFileName = 'cookie.txt';
fs.exists(cookieFileName,function(){
	var cookies = fs.read(cookieFileName);
	phantom.cookies = JSON.parse(cookies);
});
//
casper.start('http://www.weibo.com',
	{
	header : defaultheader
});
//http://weibo.com/rmrb?refer_flag=0000015010_&from=feed&loc=nickname&is_hot=1
casper.viewport(1366,768);
casper.then(function(){	casper.echo('capturex!');
	casper.capture('resultx.png');
});
casper.waitForSelector('div.username',null,
	function(){
		casper.echo('fail!');
		this.capture('resultfail.png');
		//this.exit();
	},30000);

casper.then(function(){
	casper.echo('capture!');
	casper.capture('result.png');
	this.click('div.username input');
	this.sendKeys('div.username input','***');
	this.click('div.password input');
    this.sendKeys('div.password input','***');
    this.click('a.W_btn_a');
    casper.captureSelector('resultx.png','a.W_btn_a');
	//this.click('h3.list_title_b a');
});
casper.wait(10000);
casper.then(function(){
	casper.echo('capture2!');
	casper.capture('result2.png');
	var cookies = JSON.stringify(phantom.cookies);
	fs.write("cookie.txt", cookies, "w");});

casper.run(function(){this.exit();});


