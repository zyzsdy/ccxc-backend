# CCXC Engine 部署和使用指南

CCXC Engine 是为[CCBC](https://cipherpuzzles.com)开发的Puzzle Hunt框架网站。提供用户注册和组队、题目管理、站内信和公告、答题进度追踪等举办一个Puzzle Hunt比赛所需的基本功能的平台。

CCBC (Cipher & Code Breaking Competition)是由密码菌主办的古典密码主题的线上解谜比赛，前身为百度密码吧主办的密码破译大赛。

CCXC Engine分为三个部分，分别为：
* 后端（本项目）
* [前端A（包括用户中心和后台的前端）](https://github.com/zyzsdy/ccxc-frontend)
* [前端B（看题和答题的前端）](https://github.com/zyzsdy/ccxc-frontend-next)

其中前端B推荐根据比赛的需求，通过API自行构建满足需要的版本。

这里开源的是CCBC比赛中实际使用的版本，不同届的比赛使用的不同版本可以通过Release中的Tag找到。

## 关于开发

CCXC Engine的后端是一个.NET Core项目，目前为.NET Core 3.1，已有计划迁移到.NET 6。此外，数据库使用了MariaDb，此外还使用了Redis作为辅助存储。

使用Visual Studio打开项目中的解决方案文件ccxc-backend.sln，然后Restore NuGet包即可开始开发。

请注意：如果你通过Debug启动本项目，会在Debug目录生成Config目录，然后你需要进入此目录填写配置并保存，然后重启Debug。详见关于项目的部署一节。

关于前端的构建，在代码clone到本地后，在前端目录下执行`npm i`，并等待安装完成。然后可以通过`npm run serve`(前端A)或`npm run dev`(前端B)启动本地测试服务器。

前端A使用Vue 2+Webpack构建。前端B使用Vue3+Vite构建。

实际上，前端A才是本后端所配套的前端，而前端B是完全独立构建的，根据比赛形式的不同可以自由的调整，仅需API正确调用。

## 关于部署

首先在服务器上准备好前提，你需要一个MariaDb Server（用于主数据库），一个Redis Server（用于临时数据库）和一个Nginx Server（用于网站Web服务器）。

使用Release模式发布本项目，可以通过Visual Studio的“发布”功能实现，也可以通过`dotnet publish`命令行。发布的结果会保存到指定的目录中。

注意：在发布时您必须确定您的运行平台，若是windows服务器，请生成win-x64的发布，而对于Linux平台建议生成linux-x64的发布。

复制发布生成的目录到服务器。然后尝试执行`ccbc-backend.exe`（For Windows）或者`./ccbc-backend`（For Linux）。系统会自动在当前目录下建立Config目录。

打开Config目录，你可以看到有`CcxcConfig.default.xml`和`CcxcConfig.xml`。打开`CcxcConfig.default.xml`，确定需要修改的配置并复制到`CcxcConfig.xml`中，
在`CcxcConfig.xml`中写入实际环境需要的值。一般来说，你需要修改`StartTime`的值以确定开赛时间，以及`DbConnStr`的值以确定数据库。

然后再次启动，就会根据配置中的信息自动建立数据表。

一般来说，不建议将ccxc-backend直接暴露给后端，您需要编写一个Nginx配置项，使用Nginx反代ccxc-backend，然后暴露这个Nginx的地址作为后端API地址。

然后修改前端中的[globalConst.ts](https://github.com/zyzsdy/ccxc-frontend/blob/master/src/plugins/globalConst.ts)（前端A），
或是[globalconst.js](https://github.com/zyzsdy/ccxc-frontend-next/blob/main/src/globalconst.js)（前端B）

将其中的API Root修改为后端API地址。而其中的puzzleRoot是指在主界面单击START按钮后跳转到的前端B的访问页面。具体来说，会跳转到`puzzleRoot/start?letter={token}`的目标地址。
在前端B中，可以通过解析`/start`路由上的letter属性获得主站token，然后访问后端链接鉴权来同步登录状态。

然后需要编译前端。两个前端的编译方式都是执行`npm run build`命令，在生成的dist目录中就是编译后的结果。你只需分别将两个前端的dist目录复制到服务器上，并配置Nginx指向正确的位置即可。

## 关于使用

在使用之前，你需要为自己建立一个管理员账号。首先通过注册功能注册一个账号，然后查看数据库中的`user`表，此时应该只有一个条目，就是你刚刚建立的账号。将它的`roleid`的值修改为`5`（5代表管理员）。
然后返回网站上的个人中心，任意编辑个人简介并提交（此举是为了清除缓存）。然后退出登录并重新登录，您应该可以看到导航栏里出现红色的管理员后台按钮。单击即可进入管理员后台。

在管理员后台中，有“清理缓存”的功能。有时候我们可能会直接修改数据库，因为缓存的存在，修改数据库后新数据并没有在网站上出现。这时就必须使用“清理缓存”删除对应表的缓存，然后重新访问。

接下来您可以编辑题目，上传图片，开始你的谜题创作。

要让其他人也拥有如您一样的编辑题目的权限。你需要让他们首先自己注册账号，然后你手工进入数据库中的`user`表，修改对应用户的`roleid`的值修改为`4`（4代表出题组成员）。
然后进入你自己的管理员后台，在“清理缓存”中单击“用户”。然后让他退出登录并重新登录，此时他应该已经有出题组成员的权限，可以查看和修改后台了。
（此功能今后会实现可视化界面操作，已经排上日程啦！[鸽子表情]）。

要招募测试组，你可以先让他们自行创建账号并组队，然后在后台的“用户”面板中为**每一位**点击“测试用户”列中的“设置”按钮，让他们变成测试用户。然后请他们退出并重新登录。
此时，他们的主界面上应该会多出一个不起眼的按钮，这个按钮就相当于正式开赛后的START按钮。

## 关于开源协议

本项目通过MIT协议开源。
