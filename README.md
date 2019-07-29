# NEO_Block_API
[简体中文](#zh) |    [English](#en) 

<a name="zh">简体中文</a>
## 概述 :
本项目提供了一些基础的区块链数据查询接口。例如查询块数据，交易数据等。

## 接口详情 :
我们将接口文档用小幺鸡进行了整理,详细可以参阅 _[接口文档](http://www.xiaoyaoji.cn/doc/1IoeLt6k57)_

## 部署演示 :

安装git（如果已经安装则跳过） :
```
yum install git -y
```

安装 dotnet sdk :
```
rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
yum update
yum install libunwind libicu -y
yum install dotnet-sdk-2.1.200 -y
```

通过git将本工程下载到服务器 :
```
git clone https://github.com/NewEconoLab/NEO_Block_API.git
```

修改配置文件放在执行文件下，配置文件大致如下 :
```json
{
  "mongodbConnStr_testnet": "测试网基础数据库连接地址",
  "mongodbDatabase_testnet": "测试网基础数据库连接名称",
  "neoCliJsonRPCUrl_testnet": "测试网改动后节点请求url",
  "mongodbConnStr_mainnet": "主网基础数据库连接地址",
  "mongodbDatabase_mainnet": "主网基础数据库连接名称",
  "neoCliJsonRPCUrl_mainnet": "主网改动后节点请求url",
  "mongodbConnStr_NeonOnline": "可为空",
  "mongodbDatabase_NeonOnline": "可为空",
  "startMonitorFlag": "可为空",
}
```

编译并运行
```
dotnet publish
cd NEO_Block_API/NEO_Block_API/bin/Debug/netcoreapp2.0
dotnet NEO_Block_API.dll
```

## 依赖工程 :
- [爬虫工程](https://github.com/NewEconoLab/NeoBlock-Mongo-Storage)
- [neo-cli-nel](https://github.com/NewEconoLab/neo-cli-nel) 

<a name="en">English</a>
## Overview :
This project provides some basic blockchain data query interfaces. For example, query block data, transaction data ……

## Interface details
We have compiled the interface documentation. For details, please refer to _[Interface details](http://www.xiaoyaoji.cn/doc/2veptPpn9o/edit)_

## Deployment

install git（Skip if already installed） :
```
yum install git -y
```

install dotnet sdk :
```
rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
yum update
yum install libunwind libicu -y
yum install dotnet-sdk-2.1.200 -y
```

clone to the server :
```
git clone https://github.com/NewEconoLab/NEO_Block_API.git
```

Modify the configuration file under the execution file, the configuration file is roughly as follows:
```json
{
  "mongodbConnStr_testnet": "basic database connectString at testnet",
  "mongodbDatabase_testnet": "basic database name at testenet",
  "neoCliJsonRPCUrl_testnet": "modified neo node request url",
  "mongodbConnStr_mainnet": "basic database connectString at mainnet",
  "mongodbDatabase_mainnet": "basic database name at mainnet",
  "neoCliJsonRPCUrl_mainnet": "modified neo node request url",
  "mongodbConnStr_NeonOnline": "don't care about it",
  "mongodbDatabase_NeonOnline": "don't care about it",
  "startMonitorFlag": "don't care about it",
}
```

Compile and run :
```
dotnet publish
cd NEO_Block_API/NEO_Block_API/bin/Debug/netcoreapp2.0
dotnet NEO_Block_API.dll
```

## dependency project :
- [reptile project](https://github.com/NewEconoLab/NeoBlock-Mongo-Storage)
- [neo-cli-nel](https://github.com/NewEconoLab/neo-cli-nel) 
