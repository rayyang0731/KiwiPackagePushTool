PackageTool
---

## 创建 Package 文件夹

### 通过菜单栏 Kiwi/Packages/Package 创建工具打开界面,(如果工程中有 Kiwi Utility Package, 则在编辑器扩展菜单中), 填写公司名称和 Package 名称,点击创建,即可自动生成 Package 文件夹及其内部所需文件和结构.

![img.png](Packages/Kiwi%20Package/Editor/Doc/src/1.png)
![img.png](Packages/Kiwi%20Package/Editor/Doc/src/2.png)

## Package 推送

### Project窗口中,右键 Create/Kiwi/PackagePushSetting 创建配置文件, 将 Package 的文件夹拖拽到 [Package 文件夹] 字段上, 点击推送, 即可自动根据版本号, 在当前工程的Git库中创建名为 npm 的 subtree, 并自动根据版本号打上 tag
![img.png](Packages/Kiwi%20Package/Editor/Doc/src/3.png)
