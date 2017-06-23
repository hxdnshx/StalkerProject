var casper = require('casper').create();

casper.start('www.baidu.com');

casper.then(function(){casper.echo('wow1!');});

casper.then(function(){
	casper.echo('wow2!');
	casper.then(function(){
		casper.echo('wow4!');
	});
	casper.then(function(){
		casper.echo('wow5!');
	}) 
});

casper.then(function(){casper.echo('wow3!');});

casper.run();