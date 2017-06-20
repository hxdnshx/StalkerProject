var casper = require('casper').create();
var util = require('utils');
var userName=casper.cli.args[0];
var defaultheader={
	'User-Agent': 'Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36',
	'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
	'Accept-Encoding': 'gzip, deflate, sdch',
	'Accept-Language': 'zh-CN,zh;q=0.8',
	'Host' : 'music.163.com'
};
casper.echo('connect:' + 'http://music.163.com/#/search/m/?s=' + userName + '&type=1002');
casper.start('http://music.163.com/#/search/m/?s=' + userName + '&type=1002',{
	header : defaultheader
});

casper.viewport(1366,768);

//=======================================Search User
casper.then(function(){
	casper.switchToFrame("contentFrame");
	casper.echo('PageLoaded');
	if(this.exists('a[class="txt f-fs1"]'))
	{
		this.echo("Redirect to User Page....");
		this.click('a[class="txt f-fs1"]',"80%","50%");
		this.echo(this.getCurrentUrl());
	}
	else
		this.exit();
});

//=============================================Get Status
//casper.wait(10000);
var status;
var event;
var follow;
var fan;
casper.waitForSelector('strong[id="event_count"]',null,null,10000);
casper.then(function(){
	this.echo("Load Complete");
	this.capture('wyyyy.png');
	event = this.fetchText('strong[id="event_count"]');
	follow = this.fetchText('strong[id="follow_count"]');
	fan = this.fetchText('strong[id="fan_count"]');
	casper.echo('Event:' + event + ' Follow:' + follow + ' Fan:' + fan);
	status={'event':event,'follow':follow,'fan':fan};
});

var skipcount=0;

//========================================Event
casper.thenBypassIf(function(){
	if(!event || event == 0)
	{
		skipcount+=1;
		return true;
	}
	return false;
},3);
casper.then(function(){
	this.click('strong[id="event_count"]');
	this.echo('Switch To Events');
});
var shares=new Array();
casper.waitForSelector('div[class="dcntc"]',null,null,10000);
casper.then(function(){
	var shareList=this.getElementsAttribute('div[class="dcntc"]','id');
	var i;
	for(i=0;i<shareList.length;i++)
	{
		var comment=this.fetchText('div#' + shareList[i] + ' div.text');
		var href=this.getElementAttribute(
			'div#' + shareList[i] + ' div.tit','href');
		var songName=this.fetchText(
			'div#' + shareList[i] + ' a.s-fc1');
		var songArtist=this.fetchText(
			'div#' + shareList[i] + ' a.s-fc3');
		this.echo('Comment:' + comment + ' songName:' + songName + ' songArtist:' + songArtist);
		shares.push({'songName':songName,'songArtist':songArtist,'href':href,'comment':comment});
	}
});



//==================================Follow
casper.thenBypassIf(function(){
	if(!follow || follow == 0)
	{
		skipcount+=1;
		return true;
	}
	return false;
},3);
casper.then(function(){
	this.click('strong[id="follow_count"]');
	this.echo('Switch To Follow');
});
var follows;
casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
casper.then(function(){
	follows=this.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]','title');
	this.echo('Followers:' + JSON.stringify(follows));
});



//==========================================Fans
casper.thenBypassIf(function(){
	if(!fan || fan == 0)
	{
		skipcount+=1;
		return true;
	}
	return false;
},3);
casper.then(function(){
	this.click('strong[id=fan_count]');
	this.echo('Switch To Fans');
});
var fans;
casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
casper.then(function(){
	fans=this.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]','title');
	this.echo('Fans:' + JSON.stringify(fans));
});

//====================================Return to MainList
casper.thenBypassIf(function(){return skipcount>0;},1);
casper.back();
casper.thenBypassIf(function(){return skipcount>1;},1);
casper.back();
casper.thenBypassIf(function(){return skipcount>2;},1);
casper.back();
//casper.then(function(){this.reload();});



//=====================================Song Ranking Weekly
casper.waitForSelector('div.more a');//歌曲列表之后才会加载
//Frequently Play
casper.then(function(){
	//this.capture('test.png');
	//this.captureSelector('rec.png','div.m-record');
	this.click('div.more a');
	this.echo('Switch to Weekly Songs Ranking...');
});

casper.waitWhileSelector('strong[id=fan_count]');//主界面消失
casper.waitForSelector('div.song');//加载完成

var existsWeekly=true;
casper.thenBypassIf(function(){
	if(!this.exists('span#songsweek.z-sel'))
	{
		existsWeekly=false;
		return true;
	}
	return false;
},4);

var freqWeekly=new Array();
var freqAll=new Array();
casper.then(function(){
	if(!this.exists('div.m-record'))
	{
		this.echo('Can not detect Song Ranking,skip...');
		return;
	}
	var ids=this.getElementsAttribute('div.m-record li','id');
	var i;
	for(i=0;i<ids.length;i++)
	{
		var specialSelect=function(liId,subSelector,attr){
			return casper.evaluate(function(liId,subSelector,attr){
			var liElement=document.querySelector('[id="' + liId + '"]');
			if(liElement == null)return null;
			var subElement=liElement.querySelector(subSelector);
			if(subElement == null)return null;
			return subElement.getAttribute(attr);
		},liId,subSelector,attr);
		};//因为li的ID是纯数字,所以需要使用特殊的Selector
		//Ref:https://benfrain.com/when-and-where-you-can-use-numbers-in-id-and-class-names/
		var songName=specialSelect(ids[i],'b','title');
		var songArtist=specialSelect(ids[i],'span.s-fc8 span','title');
		var percent=specialSelect(ids[i],'span.bg','style');
		percent=percent.replace(/%;/,'').replace(/width:/,'');
		freqWeekly.push({'songName':songName,'songArtist':songArtist,'percent':percent});
	}
	this.echo('\n\n\n');
	this.echo(JSON.stringify(freqWeekly));
});


//=====================================Song Ranking All
casper.then(function(){
	casper.click('span#songsall');
	this.echo('Switch to All Songs Ranking...');
});

casper.waitWhileSelector('div.song');//加载中
casper.waitForSelector('div.song');//加载完成

casper.then(function(){
	
	var ids=this.getElementsAttribute('div.m-record li','id');
	var i;
	for(i=0;i<ids.length;i++)
	{
		var specialSelect=function(liId,subSelector,attr){
			return casper.evaluate(function(liId,subSelector,attr){
			var liElement=document.querySelector('[id="' + liId + '"]');
			if(liElement == null)return null;
			var subElement=liElement.querySelector(subSelector);
			if(subElement == null)return null;
			return subElement.getAttribute(attr);
		},liId,subSelector,attr);
		};//因为li的ID是纯数字,所以需要使用特殊的Selector
		//Ref:https://benfrain.com/when-and-where-you-can-use-numbers-in-id-and-class-names/
		var songName=specialSelect(ids[i],'b','title');
		var songArtist=specialSelect(ids[i],'span.s-fc8 span','title');
		var percent=specialSelect(ids[i],'span.bg','style');
		percent=percent.replace(/%;/,'').replace(/width:/,'');
		freqAll.push({'songName':songName,'songArtist':songArtist,'percent':percent});
	}
	this.echo('\n\n\n');
	this.echo(JSON.stringify(freqAll));
});

casper.thenBypassIf(function(){return existsWeekly;},1);
casper.back();
casper.back();
//Return to Main Page

/*
var PlayLists=Array();
casper.then(function(){

	var previousPage=casper.page;
	casper.page=casper.newPage();
	//new context


	casper.page=previousPage;
});
*/

casper.run(function(){
	this.exit();
});