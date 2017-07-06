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
	//这里作为纯ID的元素的选取手段


casper.echo('connect:' + 'http://music.163.com/#/search/m/?s=' + userName + '&type=1002');
casper.start('http://music.163.com/#/search/m/?s=' + userName + '&type=1002',{
	header : defaultheader
});

casper.viewport(1366,768);

//=======================================Search User
var mainPageUrl;
casper.then(function(){casper.switchToFrame("contentFrame");});
casper.waitForSelector('a[class="txt f-fs1"]');
casper.then(function(){
	
	casper.echo('PageLoaded');
	this.echo("Redirect to User Page....");
	this.click('a[class="txt f-fs1"]',"80%","50%");
	this.echo(this.getCurrentUrl());
	mainPageUrl=this.getCurrentUrl();
});

//=============================================Get Status
//casper.wait(10000);
var status;
var event;
var follow;
var fan;
var intro;
var playCount;
var imgPath;
var nickname;
var uid;
casper.waitForSelector('strong[id="event_count"]',null,null,10000);
casper.then(function(){
	this.echo("Load Complete");
	//this.capture('wyyyy.png');
	nickname = this.fetchText('span.f-ff2.s-fc0');
	uid = this.getElementAttribute('ul.data.s-fc3 a','href').replace(/\/user\/event\?id=/,'');
	event = this.fetchText('strong[id="event_count"]');
	follow = this.fetchText('strong[id="follow_count"]');
	fan = this.fetchText('strong[id="fan_count"]');
	if(this.exists('.m-record-title'))
		playCount=this.fetchText('.m-record-title h4').replace(/累积听歌/,'').replace(/首/,'');
	else
		playCount=-1;//这里取一个特殊的数字代表最近听的歌曲不公开
	if(this.exists('dt#ava img'))
		imgPath=this.getElementAttribute('dt#ava img','src');
	intro = this.fetchText('div.f-brk').replace(/个人介绍：/,'');
	casper.echo('NickName:' + nickname + 'uid:' + uid);
	casper.echo('Event:' + event + ' Follow:' + follow + ' Fan:' + fan);
	status={'event':event,'follow':follow,'fan':fan};
});

var skipcount=0;

//========================================Event
//Note:目前因为没想清楚怎么做，只会抓取最新的几十条Event，过去的不会抓取
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
			'div#' + shareList[i] + ' a.s-fc4','href');
		var songName=this.fetchText(
			'div#' + shareList[i] + ' a.s-fc1');
		var songArtist=this.fetchText(
			'div#' + shareList[i] + ' a.s-fc3');
		this.echo('Comment:' + comment + ' songName:' + songName + ' songArtist:' + songArtist);
		shares.push({'songName':songName,'songArtist':songArtist,'href':href,'comment':comment});
	}
});



//==================================Follow
//Note：会自动翻页获取前15页的关注，也就是300个
//这样应该对于普通的解除关注行为，应该是可以察觉的吧
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
var follows=new Array();
var maxPages=15;

casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
casper.then(function(){
	if(follow/20>=maxPages)
		this.echo('拒绝抓网红的follow列表T T，只显示前'+ maxPages + '页');
	var currentPage=1;
	var getFollow=function(){
		var pageFollows=casper.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]','title');
		var i;
		for(i=0;i<pageFollows.length;++i)
		{
			follows.push(pageFollows[i]);
		}
		currentPage++;
		if(currentPage>maxPages)return;
		//casper.captureSelector('test3.png','a.znxt');
		if(casper.exists('a.zbtn.znxt') && !casper.exists('a.zbtn.znxt.js-disabled'))
		{
			casper.click('a.zbtn.znxt');
			casper.wait(100);
			casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
			casper.then(getFollow);
			casper.echo('Switch to Page '+currentPage);
		}
	};
	getFollow();
});
casper.then(function(){
	//this.echo('Followers:' + JSON.stringify(follows));
});



//==========================================Fans
//Note：粉丝的话也可以用来发现隐藏情敌（？
//这样看的话主要重点是“新增的粉丝”
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
var fans=new Array();
casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
casper.then(function(){
	if(fan/20>=maxPages)
		this.echo('拒绝抓网红的fans列表T T，只显示前'+ maxPages + '页');
	var currentPage=1;
	var getFollow=function(){
		var pageFollows=casper.getElementsAttribute('a[class="s-fc7 f-fs1 nm f-thide"]','title');
		var i;
		for(i=0;i<pageFollows.length;++i)
		{
			fans.push(pageFollows[i]);
		}
		currentPage++;
		if(currentPage>maxPages)return;
		//casper.captureSelector('test3.png','a.znxt');
		if(casper.exists('a.zbtn.znxt') && !casper.exists('a.zbtn.znxt.js-disabled'))
		{
			casper.click('a.zbtn.znxt');
			casper.wait(100);
			casper.waitForSelector('a[class="s-fc7 f-fs1 nm f-thide"]',null,null,10000);
			casper.then(getFollow);
			casper.echo('Switch to Page '+currentPage);
		}
	};
	getFollow();	
});
casper.then(function(){
	//this.echo(JSON.stringify(fans));
});

//====================================Return to MainList
casper.then(function(){this.thenOpen(mainPageUrl);});
casper.waitForSelector('.g-iframe,.m-cvrlst');
casper.then(function(){
	if(!this.exists('.m-cvrlst'))this.switchToFrame('contentFrame');});
//casper.then(function(){this.reload();});



//=====================================Song Ranking Weekly
//最近在听的歌！
//按照每个小时拉取一次的情况，可以明白每天听了什么歌
//关于每首歌曲具体的收听次数...似乎也可以带入数据算，不过意义不是特别大吧感觉...
casper.waitForSelector('div.more a',null,function(){
	this.bypass(12);//失败代表不开放这个信息，全部跳过
});//歌曲列表之后才会加载
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

		var songName=specialSelect(ids[i],'b','title');
		var songArtist=specialSelect(ids[i],'span.s-fc8 span','title');
		var percent=specialSelect(ids[i],'span.bg','style');
		percent=percent.replace(/%;/,'').replace(/width:/,'');
		freqWeekly.push({'songName':songName,'songArtist':songArtist,'percent':percent});
	}
	this.echo('\n\n\n');
	//this.echo(JSON.stringify(freqWeekly));
});


//=====================================Song Ranking All
//全歌曲其实算是信息量不太高的地方...
//主要可以分析歌曲各听了多少次吧
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
		
		var songName=specialSelect(ids[i],'b','title');
		var songArtist=specialSelect(ids[i],'span.s-fc8 span','title');
		var percent=specialSelect(ids[i],'span.bg','style');
		percent=percent.replace(/%;/,'').replace(/width:/,'');
		freqAll.push({'songName':songName,'songArtist':songArtist,'percent':percent});
	}
	this.echo('\n\n\n');
	//this.echo(JSON.stringify(freqAll));
});

casper.thenBypassIf(function(){return existsWeekly;},1);
casper.back();
casper.back();
//Return to Main Page


var PlayLists=new Array();
casper.waitForSelector('ul#cBox a.tit.f-thide.s-fc0');
casper.then(function(){
	//this.capture('test3.png');
	var links=this.getElementsAttribute('ul#cBox a.tit.f-thide.s-fc0','href');
	var iter;
	this.echo('playList Count:' + links.length);
	for(iter=0;iter<links.length;iter++)
	{

		var link=site + links[iter];
		this.thenOpen(link);
		//this.waitForSelector('iframe#contentFrame');
		this.then(function(){this.switchToFrame('contentFrame');});
		this.waitForSelector('table.m-table,i.u-icn-21');//List or NotFound
		this.then(function(){
			
			var playListName=this.fetchText('h2.f-ff2.f-brk');
			var playCount=this.fetchText('strong#play-count');
			var description=this.fetchText('p.intr');
			var favCount=this.getElementAttribute('a.u-btni-fav','data-count');
			var commentCount=this.fetchText('span#cnt_comment_count');
			var id = this.getCurrentUrl().replace(/http:\/\/music\.163\.com\/playlist\?id=/,'');
			this.echo('\n\nplayList:' + playListName + ' id:' + id + '  playCount:' + playCount + ' Fav:' + favCount + ' Comment:' + commentCount);
			this.echo('description:' + description);
			var musicList=null;
			var songData=new Array();
			var i;
			if(!this.exists('i.u-icn-21'))
			{
				musicList=this.getElementsAttribute('table.m-table tr','id');
				for(i=0;i<musicList.length;i++)
				{
					var songName=specialSelect(musicList[i],'div.f-cb div.tt div.ttc span.txt a b','title');
					var songId=specialSelect(musicList[i],'div.f-cb div.tt div.ttc span.txt a','href').replace(/\/song\?id=/,'');
					if(songName == null || songName == '')continue;
					songData.push({'songName' : songName, 'id' : songId});
					this.echo('song:' + songName);
				}
			}
			PlayLists.push({'id' : id,'playList' : playListName,'playCount' : playCount,'description' : description,'favCount' : favCount,'commentCount':commentCount,'musicList' : songData});
		});
		this.back();
	}
});


casper.then(function(){
	//this.echo(JSON.stringify(PlayLists));
});

var fs=require('fs');

casper.run(function(){
	var result={
		'status' : {
			'event' : event,
			'follow' : follow,
			'fan' : fan,
			'intro' : intro,
			'playCount' : playCount,
			'imgPath' : imgPath,
			'name' : nickname,
			'uid' : uid
		},
		'shares' : shares,
		'follows' : follows,
		'fans' : fans,
		'freqWeekly' : freqWeekly,
		'freqAll' : freqAll,
		'playLists' : PlayLists
	};
	fs.write(casper.cli.args[1],JSON.stringify(result));
	this.echo('Finish. Exported to' + casper.cli.args[1]);
	this.exit();
});