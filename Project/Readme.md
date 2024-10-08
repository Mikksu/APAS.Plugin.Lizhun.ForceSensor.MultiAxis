﻿# 压力传感器插件使用指南



## 界面说明

![mainFrame](\Resources\mainFrame.png)

操作界面元素说明如下：

| 项目         | 说明                                                         |
| ------------ | ------------------------------------------------------------ |
| LD电流（A）  | LD驱动电流，单位 A。注意：LD电流输出使用2600的`通道A`        |
| Vf（V）      | LD前向电压实时值，单位 V                                     |
| PD电流（uA） | mPD监控电流实时值，单位 uA。注意：mPD电流监测使用2600的`通道B` |
| A-ON         | 打开通道A输出                                                |
| A-OFF        | 关闭通道A输出                                                |
| B-ON         | 打开通道B输出                                                |
| B-OFF        | 关闭通道B输出                                                |



## 如何通过界面控制SMU

### 设置LD电流

在文本框内输入电流值，然后点击右侧的“设置”按钮，将电流写入SMU。



### 打开或关闭输出

点击按钮 `A-ON`  或 `A-OFF` 可打开或关闭`通道A`的输出。

点击按钮 `B-ON`  或 `B-OFF` 可打开或关闭`通道B`的输出。



 ## 如何使用脚本操作

可通过两种方式进行控制：

-  APAS 的图形化脚本编辑器

- 外部脚本



### 通过图形化脚本编辑器

#### 设置SMU参数及控制输出

1. 打开 `脚本编辑器`
2. 左侧的指令选择窗口里找到 `插件` -> `插件控制` ，并将其拖入脚本列表
3. 在脚本列表中选中刚才拖入的 `插件控制` 指令
4. 在右侧的属性窗口中找到 `插件` 栏位，并选择 `KEITHLEY 2600`
5. 在 `参数` 栏位中填写控制参数


> 关于如何填写 *控制参数*，请参考 *通过外部脚本* 章节



#### 读取PD电流

1. 打开 `脚本编辑器`
2. 左侧的指令选择窗口里找到 `变量` -> `设备测量` ，并将其拖入脚本列表
3. 在脚本列表中选中刚才拖入的 `设备测量` 指令
4. 在右侧的属性窗口中找到 `设备` 栏位，并选择 `KEITHLEY 2600` 
5. 在 `变量名` 栏位中填写用于保存测量值的变量名称



### 通过外部脚本

#### 设置电源参数及控制输出

在C#脚本中调用下述函数，可对本插件进行操作：

```c#
apas.__SSC_EquipmentPluginControl("KEITHLEY 2600", "参数");
```



关于支持的 `参数`，请参考下表：

| 参数                 | 说明                   | 示例                                                         |
| -------------------- | ---------------------- | ------------------------------------------------------------ |
| ON [A&#124;B&#124;ALL]    | 打开指定通道或所有通道输出 | // 打开通道A输出<br />apas.__SSC_EquipmentPluginControl("KEITHLEY 2600", **"ON A"**); |
| OFF [A&#124;B&#124;ALL]   | 关闭指定通道或所有通道 | // 关闭通道B电源输出<br />apas.__SSC_EquipmentPluginControl("KEITHLEY 2600", **"OFF B"**); |
| CURR value | 设置LD驱动电流   | // 将LD驱动电流设置为 300mA<br />apas.__SSC_EquipmentPluginControl("KEITHLEY 2600", **"CURR 0.3"**); |



#### 读取PD电流

```c#
// 读取PD电流
apas.__SSC_MeasurableDevice_Read("KEITHLEY 2600");
```

