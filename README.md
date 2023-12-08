# TG 台股查詢機器人 基本使用教學  :memo:

使用方法：

1.下載後將appsettings裡的APIkey換成自己的API Key後執行檔案即可使用

2.(非必要)打包成Docker使用，Dockerfile已經寫好了，直接使用下面指令build即可
  Docker build 指令
```cmd
  docker build -t 名稱 . --no-cache
```

展示用機器人ID
```cmd
https://t.me/Tian_Stock_bot
```

## 機器人指令

⭐️K線走勢圖
```cmd
/k 2330 d
h - 查詢時K線
d - 查詢日K線
w - 查詢週K線
m - 查詢月K線
5m - 查詢5分K線
10m - 查詢10分K線
15m - 查詢15分K線
30m - 查詢30分K線
60m - 查詢60分K線
```
⭐️股價資訊
```cmd
/v 2330 
```
⭐️績效資訊
```cmd
/p 2330 
```
⭐️個股新聞
```cmd
/n 2330
```

## 使用TradingView查詢

⭐️查看圖表
```cmd
/chart 2330
```

⭐️選擇週期範圍
```cmd
/range 2330
```
