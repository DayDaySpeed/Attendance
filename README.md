软件下载地址[AttendanceInstaller.exe]：

https://gitee.com/statry/attendance/releases/tag/v1.0 [gitee]



数据库用的时SQLite，保存的数据库文件在根目录下的db文件夹，里面有两个.db分别是poem.db和class.db

用户背景配置用的是json保存，文件也在根目录下，名为user_theme.json

## 点名系统

#### 导入：

支持EXCEL和CSV导入，excel表头只要有名字或学号就能导入，自动检索表头

完整表头为**学号，名字，性别**，不限顺序

如果导入的数据不完整，那么会被赋值为默认值，学号默认值为0，名字默认为空字符串，性别默认为null

#### 抽取：

抽取动画我只单独定义了抽一人，抽二人，抽三人的，剩下的四到十人都采用相同的动画

抽取设置：可以设置人数，学号末尾数字，和性别
<img width="2559" height="1364" alt="屏幕截图 2025-10-04 204445" src="https://github.com/user-attachments/assets/6cd3c628-5c83-45fd-90ca-6c22dbe67247" />

## 诗词系统

引用的项目 https://github.com/caoxingyu/chinese-gushiwen 作为数据库数据来源

### 诗词分为展示板块和搜索板块

### 展示板块

展示采用垂直排列横向布局，模拟出有诗意氛围的滚动效果，随机展示数据库中的诗词

操作：可以刷新，鼠标悬停停止滚动

<img width="655" height="521" alt="屏幕截图 2025-10-04 193500" src="https://github.com/user-attachments/assets/b80b125c-91c3-4dbe-b1a8-93af659fcd4c" />


#### 搜索板块

搜索支持三种方式搜索，**诗词搜索，作者搜索，名句搜索**，对应数据库的三张表

诗词从标题和内容搜，作者从作者名字和生平搜，名句从内容和来源搜

展示50条搜索结果

<img width="896" height="461" alt="屏幕截图 2025-10-04 204333" src="https://github.com/user-attachments/assets/64c08427-fff2-41e7-a36e-3d381c9036f2" />

## 天气系统

天气API依赖[和风天气 ](https://www.qweather.com/)

主要展示一些免费的服务，有温度，日出日落，空气质量，月相，太阳高度角，预警信息，和天气降水预报

<img width="654" height="1294" alt="屏幕截图 2025-10-04 194403" src="https://github.com/user-attachments/assets/75b9d01f-b785-4a8f-b0d4-a8dcb2f0485e" />

时间主要展示，系统时间，日期，星期，IP
<img width="654" height="1294" alt="屏幕截图 2025-10-04 194403" src="https://github.com/user-attachments/assets/90ee783a-6ded-4ee9-b961-54868b810bb7" />


## 每日一言系统

### 分为每日诗词和每日一言

**每日诗词**来源数据库的sentence表，随机抽取可刷新

**每日一言**来源APIhttps://v1.hitokoto.cn/

<img width="713" height="522" alt="屏幕截图 2025-10-04 220111" src="https://github.com/user-attachments/assets/257e31cd-516d-46ee-be97-0133d00e5f89" />


## 整体一览

**每条 分割线都可以拉伸，可以仅仅展示感兴趣的区域，支持浅深两种主题，还可以导入背景**

<img width="2559" height="1367" alt="屏幕截图 2025-10-04 204824" src="https://github.com/user-attachments/assets/cab68279-517f-4c39-bc17-5a8cd7e2a2ee" />

