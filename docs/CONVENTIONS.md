# 高难度题库 · 分层命名与防作弊约定(平台侧内部约定)

> 本文件为平台侧约定文档,面向索引构建与审题代理,**不进入答题者包**。
> 对应实施计划 `docs/superpowers/plans/2026-10-08-harder-topics.md` 的 Global Constraints。

---

## 1. 目录与命名空间约定

- 每题一个领域库工程:`topics/Shishan.Domain.XXX/`(XXX = 题代码,如 D001、K005)。
- 每题一个测试工程:`tests/Shishan.Tests.XXX/`,判分测试(期望表现)写于此。
- 每题一个答案工程:`solutions/Shishan.Solution.XXX/`,同结构修复版(参考实现),平台侧隐藏。
- 命名空间固定:
  - 领域库:`Shishan.Domain.XXX`。
  - 测试:`Shishan.Tests.XXX`。
- 测试与答案在移交答题者前保持隐藏;答题者只接触 `topics/Shishan.Domain.XXX/`。

## 2. 分层命名约定(每题领域库按此分层组织,8~12 文件)

| 层 | 文件命名 | 职责 |
|---|---|---|
| 实体 / 值对象 | `<Entity>.cs` / `<ValueObject>.cs` | 领域实体与值对象 |
| 枚举 / 分类 | `<State|Kind|Result>.cs` | 枚举 / 分类 |
| 仓储 / 索引 | `<Store>.cs` | 仓储 / 索引(缺陷常埋点) |
| 规则 / 换算 / 校验 | `<Policy|Calculator|Validator>.cs` | 规则 / 换算 / 校验(缺陷常埋点) |
| 视图 / 转换 | `<Mapper|View|Dto>.cs` | 视图与转换(缺陷常埋点) |
| 业务入口 | `<Service>.cs` | 公开业务入口(判分测试仅调此层) |
| 错误 | `<Errors|Exception>.cs` | 领域错误 |

- 方法 / 字段命名须为朴素业务语义,不得暗示缺陷方向或实现机制。
- 允许 C# `lock` 语句与简报指定的裸 `Thread.Sleep(1)`;两者不视为提示。

## 3. 防作弊规则(逐条强制)

- 每题领域库:8~12 文件、1200+ 行(不含 obj/bin)。
- 领域库 `Shishan.Domain.XXX.csproj` **禁止** `ProjectReference` 任何 test/solution/src 项目。
- 命名空间固定 `Shishan.Domain.XXX`;测试命名空间 `Shishan.Tests.XXX`。
- 测试仅调用领域库**公开 API**,禁止反射/内部字段断言。
- 注释中性:零技术名词(并发/异步/并行/幂等/缓存/快照/协议/状态机/投影/回写/隔离/原子/线程/队列/事务/锁/串行/全局累加 等),零 TODO/FIXME/HACK,零"此处有 bug"痕迹;允许"同时/几个人同时"等业务措辞。
- 缺陷必须**确定性**触发:题目版稳定红、答案版稳定绿,禁止 flaky(并发缺陷用 Barrier + 放大窗口)。
- `dotnet build Shishan.sln --t:Rebuild` 0 错 0 警。
- 答题者包 = 仅 `topics/Shishan.Domain.XXX/` + 业务描述;不含 tests/solutions/src/git 历史。
- 本题库的缺陷语义**不得**与第三方同名赛事原题(与本仓库无关)文案一致,场景与代码须改写。

## 4. 评审关注点

- **多组输入反硬编码**:每个公开 API 的测试须用 ≥3 组不同输入,防按单值特化。
- **错误/边界分支**:空集合、单元素、首/尾、非法参数、重复调用——缺陷常藏于此。
- **确定性与并发**:任何"同时操作"用例必须在 CI 负载下稳定复现(逐题 30 连跑验证)。
- **诱饵不得成为答案点**:死代码/误导命名必须真的不参与判定。
- **防作弊完整性**:领域库项目文件不得出现对 tests/solutions 的任何引用或路径。

## 5. 打包答题者包

```powershell
# 在仓库根执行;仅导出单个题目领域库
powershell -ExecutionPolicy Bypass -File tools/make-solver-package.ps1 -Topic D002 -Out <目标目录>
```

- 输出目录仅含 `Shishan.Domain.XXX/` 下领域库源码 + csproj,**不含** tests/solutions/src/.git/obj/bin。
- 副本须可独立 `dotnet build` 通过(不依赖判分测试与答案)。