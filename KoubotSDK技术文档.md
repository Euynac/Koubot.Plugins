# Koubot SDK技术文档

[TOC]

使用Koubot SDK来开发Koubot的插件，可以实现多平台兼容，同时实现多种自带功能（使用命令机制调用插件、自动生成插件帮助、命令别名、内置参数、（通过UI或命令进行）插件管理、异常处理等等），SDK提供丰富的接口与工具类辅助开发，提供数据库自动模型基类扩展数据库。

Koubot不是事件驱动型，而是数据驱动型，在插件实现过程中，更多的是思考数据的维护和处理，而无需过多考虑事件的监听和变化；开发插件无需考虑机器人位于什么平台，消息是如何传递等等，只需要专注于Koubot环境下的消息逻辑处理。



## 映射特性标签

特性标签就是指的C#中的一个特性（Attribute）语法，取名叫映射特性标签，是因为它在Koubot框架中是用于映射的特点，比如对于命令与插件类而言，命令中的插件名、功能名、参数名，恰好是一一映射插件类中的插件类、类中的方法、类中的属性。又对于命令与模型类而言，模型类中的类与属性，是映射命令中的表名与字段名。

提供了很多设置，可以直接从IDE自动提示中获得，包含详细注释，或查询SDK API文档（暂未开放）。

### 插件类

#### 类 —— 插件 KouPluginClass

该特性标签仅适用于类，指示该类是插件的映射，命令调用这个插件将会作用到这样一个类。

#### 方法 —— 功能 KouPluginFunction

该特性标签仅适用于方法，指示该方法是插件中功能的映射，命令调用这个插件下的功能将会作用到这样一个具体方法。

> 注意：命令中不声明功能名将会默认调用的是插件类中的默认方法，默认方法接收所有隐式参数成为一个string。

#### 方法参数 —— 隐式参数 KouPluginArgument

该特性标签仅适用于方法中的参数，指示该方法参数是插件中功能隐式参数的映射，命令赋值这个插件下的隐式将会作用到这样一个具体的方法参数。

> 该标签没有激活名，因此一般可以不用对功能参数进行打标签，但为了自动生成的帮助更加容易理解，最好还是打上标签。

#### 属性 —— 显式参数 KouPluginParameter

该特性标签仅适用于类中的属性，指示该属性是插件中参数的映射，命令赋值这个插件下的显式参数将会作用到这样一个具体的属性。

### 数据库自动模型

#### 类 —— 表 KouAutoModelTable

该特性标签仅适用于继承于KouAutoModel的类，特性中的激活名和绑定的插件反射名需要同时使用，这样才可以唯一确定一个表（比如绑定arcaea插件，插件激活名为arc，该表激活名为song，则用命令映射表即为`/arc.song`）

#### 属性 —— 字段 KouAutoModelField

该特性标签仅适用于继承KouAutoModel类中的属性，在使用数据库自动模型命令模式下，命令中的显式参数映射到了该字段，也是该类的属性上。





## 接口与基类

提供了很多接口与基类辅助开发。

### 插件类

#### KouPlugin基类

Koubot里面的插件实际上都是一个类，然后每次调用插件都会实例化一个这样的类，所以插件类需要继承这个基类。该基类本身实现了IKouError接口，即可以使用Kou错误与异常处理系统。

##### IWantKouMessage 获取原始消息（包含来源、用户等信息）接口

需要来源用户ID、来源群的插件需要实现这个接口，那么在调用时会注入原始信息实例，原始信息本身也提供很多API，比如快速按照来源回复、对用户进行禁言、私聊回复等等。

##### IWantTargetGroup 获取--g内置参数目标群组接口

指示该插件类支持--g参数，用户使用--g参数指定的目标群组将会传入到插件类中进行处理。

##### IWantKouSession 获取会话服务接口

指示该插件支持会话服务

##### IWantCommandLifeKouContext 获取命令生命周期型KouContext接口

为了更好的支持EFCore中的LazyLoad而诞生的接口。

##### IWantKouCommand 获取命令实例接口

KouCommand中有大量的API可以使用。



### 数据库模型类

#### KouAutoModel\<T\>基类

该基类将提供安装到Koubot数据库的方法（基于EFCore），以及数据库自动模型系统的各个功能。



#### IKouFormattable 格式化接口

> 如果使用数据库自动模型系统，不需要再实现该接口

使用原装ToString方法来格式化一个Model类，增强了原始IFormattable接口的功能，其虽然灵活多变但是是根据string来判断格式，这个接口改用的是使用Enum来进行格式选择，这样减少了不可控性，并加强了代码规范性。

而且，提供了该接口的扩展方法，能够非常迅速的格式化List。

使用maimai中歌曲表数据的实现示范（其中的`XX?.Be`是Koubot.Tool中的扩展方法，能够非常方便的进行格式化，即如果为null则不进行格式化，另外还有其他的Be类格式化方法，详见Koubot.Tool）

```c#
public override string ToString(FormatType format, object supplement = null)
        {

            bool onlySplashData = SongGenre == null;
            string splashAndDxData = $"{SongGenre?.Be($"\n分类：{SongGenre}")}{ToRatingString()?.Be("\n难度：$0", true)}{ToConstantString()?.Be("\n定数：$0", true)}" +
                $"{ToSplashRatingString()?.Be("\nSplash难度：$0", true)}";

            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongId}.{SongTitle}({SongType}) {(onlySplashData ? $"*[{ToSplashRatingString()}]" : $"[{ToConstantString()}]")}";

                case FormatType.Detail:
                    return $"{JacketUrl?.Be(new KouImage(JacketUrl, this).ToKouResourceString())}" + 
                           $"{SongId}.{SongTitle} [{SongType}]" +
                           splashAndDxData +
                           SongArtist?.Be($"\n曲师：{SongArtist}") +
                           SongBpm?.Be($"\nBPM：{SongBpm}") +
                           SongLength?.Be($"\n歌曲长度：{SongLength}") +
                           Remark?.Be($"\n注：{Remark}");

            }
            return null;
        }
```




