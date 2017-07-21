# StalkerProject
## 介绍
这是一个用于对若干个SNS平台所获取的信息，进行总合，汇集，以特定的方式（如每日邮件，RSS，总合网页）进行展现，以提高信息获取效率的项目。
~~设置成他人后可以变相进行STK，不过这样做大概会孤独一生的！~~

计划采用React架设前端，nginx+C#(mono)架设后端，对于部分页面的抓取通过casperjs进行。

## 进度
1. 抓取目标
 - [x] 网易云（完成）（[netease.js](https://github.com/hxdnshx/StalkerProject/blob/master/netease.js)）
 - [x] 念（除图片拉取全部完成）（登录相关参考[nian-Robot](https://github.com/ConnorNowhere/nian-robot)）（[API-nian.so](https://github.com/hxdnshx/StalkerProject/blob/master/API-nian.so)）(直接使用网页抓取,可能会增加浏览计数)
 - [ ] 微博(可登录)
 - [ ] QQ（待定）
 - [ ] 微信朋友圈（待定）
2. 数据分析	
 - [x] 网易云（进行增量Diff，输出新的数据）
 - [x] 念(进行增量Diff,输出新的数据)
 - [ ] 微博
 - [ ] QQ（待定）
 - [ ] 微信朋友圈（待定）
 - [ ] 腾讯语义分析对接
3. 数据输出
 - [x] 邮件(完成!)
 - [x] RSS(完成!)
 - [ ] 网页报表
4. 工程部署
 - [ ] docker
5. 系统管理
 - [ ] WinForm界面
 - [ ] 网页界面
 - [ ] 服务稳定性监视

