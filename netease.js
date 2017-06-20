var casper = require('casper').create();
var util = require('utils');
var userName=casper.cli.args[0];
var defaultheader={
	'User-Agent': 'Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36',
	'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
	'Accept-Encoding': 'gzip, deflate, sdch',
	'Accept-Language': 'zh-CN,zh;q=0.8'
};
casper.echo('connect:' + 'http://music.163.com/#/search/m/?s=' + userName + '&type=1002');
casper.start('http://music.163.com/#/search/m/?s=' + userName + '&type=1002',{
	header : defaultheader
});

casper.viewport(1366,768);

casper.then(function(){
	casper.switchToFrame("contentFrame");
	casper.echo('PageLoaded');
	if(this.exists('a[class="txt f-fs1"]'))
	{
		this.echo("Redirect to User Page....");
		this.captureSelector('wytest.png','a[class="txt f-fs1"]');
		this.click('a[class="txt f-fs1"]',"80%","50%");
	}
	else
		this.exit();
});

var status;
casper.then(function(){
	this.echo("Load Complete");
	this.capture('wyyyy.png');
	var event = this.fetchText('strong[id="event_count"]');
	var follow = this.fetchText('strong[id="follow_count"]');
	var fan = this.fetchText('strong[id="fan_count"]');
	casper.echo('Event:' + event + ' Follow:' + follow + ' Fan:' + fan);
	status={'event':event,'follow':follow,'fan':fan};
});

//Event

casper.then(function(){
	this.click('strong[id="event_count"]/..');
	this.echo('Switch To Events');
});
var shares=new Array();
casper.then(function(){
	var shareList=this.getElementsAttribute('div[class="dcntc"]','id');
	var i;
	for(i=0;i<shareList.length;i++)
	{
		var comment=this.fetchText('div[id="' + shareList[i] + '"]/div[class="text f-fs1  f-brk j-text"]');
		var href=this.getElementsAttribute(
			'div[id="' + shareList[i] + '"]/div[class="tit f-thide f-fs1"]','href');
		var songName=this.fetchText(
			'div[id="' + shareList[i] + '"]/div[class="tit f-thide f-fs1"]');
		var songArtist=this.fetchText(
			'div[id="' + shareList[i] + '"]/div[class="from f-thide s-fc3"]');
		this.echo('Comment:' + comment + ' songName:' + songName + ' songArtist:' + songArtist);
		shares.push({'songName':songName,'songArtist':songArtist,'href':href,'comment':comment});
	}
});

//Follow
casper.then(function(){
	this.click('strong[id="follow_count"]/..');
	this.echo('Switch To Follow');
});
var follows;
casper.then(function(){
	follows=this.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]',title);
	this.echo('Fans:' + JSON.stringify(follows));
});

//Fans
casper.then(function(){
	this.click('strong[id=fan_count]/..');
	this.echo('Switch To Fans');
});
var fans;
casper.then(function(){
	fans=this.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]',title);
	this.echo('Fans:' + JSON.stringify(fans));
});

casper.run(function(){
	this.exit();
});