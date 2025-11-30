## Task #2 - Part 2：使用 Serverless 架构集成其他 AWS 云服务

### 1. 总体说明（Overview）

在 Task #2 的 Part 2，我们小组会在**现有的基于服务器的房产管理系统**（ASP.NET Core MVC 应用 `PropertyWeb` + AWS RDS SQL Server）基础上，集成一个**简单但可运行的 serverless 架构**，主要使用：

- **Amazon API Gateway**
- **AWS Lambda**
- **Amazon Simple Storage Service (S3)**

目标是：把部分功能从传统的服务器端应用中拆出来，交给 serverless 组件处理，一方面引入 **microservices（微服务）** 的概念，另一方面展示系统如何在云原生（cloud-native）环境下更好地扩展和演进。

---

### 2. 选定用例：Maintenance Ticket Image Upload Service（报修工单图片上传服务）

我们计划实现一个基于 serverless 的 **Maintenance Ticket Image Upload Service（报修工单图片上传服务）**。目前系统已经可以在 RDS 中存储房产和用户信息，但还没有一个标准化的“报修流程”，例如：房门破损、水管漏水等问题，由租客在线提交 ticket 并附上现场照片。

新的服务将实现：

- 租客 / 用户通过原有的 `PropertyWeb` 网站提交 **维修报修工单（ticket）**，例如“房门破损”。
- 在提交 ticket 的同时，可以上传现场损坏照片作为证据和参考。
- 系统通过 **API Gateway + Lambda** 生成 **S3 的 pre-signed URL（预签名上传链接）**，让前端浏览器可以**直接上传报修图片到 S3**。
- （可选）将图片的元数据（例如 S3 key、ticket ID、property ID 等）存储到数据库中，用于后续查看、维修跟进和历史记录管理。

这样的设计可以：

- 支持一个完整的 **线上报修流程**：用户提交 ticket + 描述问题 + 上传损坏照片。
- 降低 Web 服务器的负载，避免大文件上传流量都经过 EC2 实例。
- 引入一个职责清晰、独立的“报修图片处理微服务”。
- 展示如何在现有系统中实际集成 serverless 技术。

---

### 3. 高层架构设计（High-Level Architecture，Part 2 重点）

在 Part 2 中，我们的扩展架构包含以下关键组件：

- **Client（浏览器）**  
  继续使用现有的 `PropertyWeb` 前端界面提交报修工单，并上传报修图片（例如房门破损照片）。

- **Server-Based Application（`PropertyWeb`）**  
  - 已实现的 ASP.NET Core MVC 应用。  
  - 仍然部署在服务器（如 EC2）上，并连接 **AWS RDS SQL Server** 存储核心业务数据（包括用户、房源和报修 ticket 等）。  
  - 新增能力：调用 **Amazon API Gateway** 提供的 API，与 serverless 报修图片服务交互。

- **Amazon API Gateway（REST API）**  
  - 对外暴露 REST 接口，例如 `POST /tickets/images/presigned-url`。  
  - 接收来自 `PropertyWeb` 的请求，并以 Lambda proxy integration 方式转发给 Lambda 函数。

- **AWS Lambda Function(s)**  
  - 使用 AWS Lambda 支持的语言实现（例如 .NET 或 Node.js）。  
  - 负责为报修图片上传生成 **S3 pre-signed URL**。  
  - 可选：将图片元数据写入 RDS 或其他数据存储（例如 `MaintenanceTickets` 及其关联图片表）。

- **Amazon S3 Bucket（例如 `property-ticket-images-bucket`）**  
  - 存储报修工单相关的原始图片（例如房门破损、水管漏水等问题的现场照片，以及可选的缩略图、处理后图片等）。  
  - 使用 Lambda 生成的 pre-signed URL，由客户端直接上传到该 bucket。

通过这一设计，Part 2 明确满足要求：**“在现有基于服务器的架构中集成一个简单可运行的 serverless 架构，使用 Amazon API Gateway 和 AWS Lambda，并结合至少一个（S3/SNS/SQS，我们选择 S3）云服务。”**

---

### 4. Maintenance Ticket Image Upload Service 的详细流程

#### 4.1 获取 Pre-Signed URL（预签名链接）

1. 租客在现有的 `PropertyWeb` 网站中进入“报修 / 提交工单”页面，选择问题类型（例如“房门破损”），并准备上传现场照片。  
2. `PropertyWeb` 在服务器端使用 `HttpClient` 等方式，向 **API Gateway** 的端点发送 HTTP 请求，例如：  
   - `POST https://{api-id}.execute-api.{region}.amazonaws.com/prod/tickets/images/presigned-url`
3. 请求体中会包含：  
   - `ticketId`（报修工单 ID，或由 propertyId + 用户信息生成）  
   - `propertyId`（房源 ID）  
   - `fileName`（文件名）  
4. **API Gateway** 将该请求转发给 **Lambda** 函数。  
5. **Lambda** 使用 AWS SDK for S3：  
   - 为特定 S3 对象 key 生成一个 **pre-signed PUT URL**，例如：`tickets/{ticketId}/{guid}.jpg`，bucket 为 `property-ticket-images-bucket`。  
6. Lambda 将 JSON 响应返回给 API Gateway，再由 API Gateway 返回给 `PropertyWeb`，其中包含：  
   - `uploadUrl`（预签名上传 URL）  
   - `key`（S3 对象 key）

#### 4.2 报修图片上传到 S3

1. `PropertyWeb` 将 `uploadUrl` 和 `key` 返回给前端浏览器（通过 MVC View 或前端 API）。  
2. 浏览器使用 JavaScript（例如 `fetch` 或 `axios`）对 S3 发起 **直接 PUT 请求**，请求 URL 即为 `uploadUrl`，请求体为报修图片文件数据。  
3. S3 校验 pre-signed URL 后，将图片持久化保存到指定 bucket，并与对应的报修工单（ticket）关联起来。  

#### 4.3 存储报修图片元数据（可选增强）

为了更好地体现 microservices 概念，我们设计一个可选增强功能：

- 新增一个 API Gateway 端点，例如 `POST /tickets/images/metadata`：  
  1. 接收 `ticketId`、`propertyId`、`s3Key`、`fileName`、时间戳等信息。  
  2. 触发另一个 Lambda 函数，将这些信息写入 RDS（例如写入 `TicketImages` 表，或与 `MaintenanceTickets` 表建立一对多关系）。  

另一种做法是：`PropertyWeb` 使用从 pre-signed URL 请求中返回的 `s3Key`，直接更新 RDS 中对应的 ticket 记录。我们会根据时间和复杂度选择合适方案，同时保证能体现 serverless 集成的核心思路。

---

### 5. 额外 AWS 功能的使用（可选 / Future Work）

为了进一步增强方案（以及作为加分项或后续改进），我们可以：

- 在 S3 bucket 上配置 **S3 event notifications**，当有新的报修图片上传时触发另一个 Lambda：  
  - 自动生成图片缩略图，并保存到同一 bucket 中的 `thumbnails/` 前缀。  
  - 或向 **Amazon SNS** / **Amazon SQS** 发送消息，用于异步处理（例如向维修人员推送通知、更新统计分析等）。  

这些扩展功能不是完成 Part 2 的必需内容，但可以展示我们对 serverless 和事件驱动（event-driven）架构有更深入的理解。

---

### 6. 如何满足 Task #2 Part 2 的要求

我们的 Part 2 实现将从以下几方面满足作业要求：

1. **在现有 server-based 系统中集成 serverless 架构**  
   - 原有的 ASP.NET Core MVC 应用 `PropertyWeb` 仍然是主 Web 前端，并继续使用 RDS 存储包括报修 ticket 在内的业务数据。  
   - 新增一个独立的 serverless 组件（API Gateway + Lambda + S3）专门负责报修图片相关处理。

2. **使用 Amazon API Gateway 和 AWS Lambda**  
   - API Gateway 暴露与报修图片相关的 REST 接口（例如获取 pre-signed URL、提交元数据等）。  
   - Lambda 函数由这些接口触发，执行 S3 相关操作，并在需要时进行数据库写入。

3. **与至少一个额外 AWS 云服务（S3）集成**  
   - 使用 Amazon S3 存储通过 Lambda 生成的 pre-signed URL 上传的报修图片（如房门破损照片）。  
   - 明确满足“serverless 架构需要结合 S3/SNS/SQS 中至少一个服务”的要求（我们明确选择 S3）。  

整体设计在实现复杂度和时间成本之间做了平衡：既简单易于落地，又足以清晰展示 serverless 集成、microservices 思想，以及 AWS 云服务在真实业务场景（线上报修 + 图片上传）中的实际应用。

---

### 7. AWS 资源蓝图（Resource Blueprint）

为了确保后续可以快速在不同 AWS 账号/区域重建 Part 2 架构，我们在文档中落地一套最小可运行（MVP）的资源清单（默认 Region 使用 `ap-southeast-1`，可按需调整）：

1. **S3 Bucket：`property-ticket-images-${env}`**
   - 用途：存放报修图片原始文件，future work 可扩展缩略图、处理结果。
   - 配置：启用版本化（versioning）与默认 SSE (SSE-S3 或 SSE-KMS)；CORS 允许来自 `PropertyWeb` 域名的 `PUT`。

2. **API Gateway REST API：`PropertyTicketImageApi`**
   - Stage：至少 `dev`、`prod` 两个 stage，对应不同环境。
   - 主要资源/方法：
     - `POST /tickets/images/presigned-url`
     - （可选）`POST /tickets/images/metadata`

3. **Lambda Functions**
   - `GeneratePresignedUrlFunction`
     - Runtime：`.NET 8` 或 `Node.js 20.x`（最终根据组员擅长决定，默认 Node.js 20）。
     - 入口：`app.handler`
     - 依赖：`AWSSDK.S3` 或 `@aws-sdk/client-s3`。
   - （可选）`PersistTicketImageMetadataFunction`
     - 访问 RDS 写入 `TicketImages` 表，使用 VPC 连接或 RDS Proxy。

4. **IAM 角色与策略**
   - `LambdaExecutionRole`：授予 S3 `PutObject`, `GetObject`, `ListBucket`（按 key 前缀限制）、CloudWatch Logs 权限。
   - （可选）访问 RDS 时，再附加 Secrets Manager / Systems Manager Parameter Store 的读取权限。
   - API Gateway 授权方式：先使用 `IAM` 或 `API Key`，后续可扩展 Cognito/JWT。

5. **Networking & Config**
   - RDS 连接信息存储于 `AWS Secrets Manager`（secret 名称 `PropertyWebRdsSecret-${env}`）。
   - Lambda 如需进 VPC，选择私有子网 + 安全组，允许访问 RDS。

6. **Automation**
   - 建议使用 Terraform 或 AWS CDK：一个 stack 输出 API Gateway URL、Lambda ARN、S3 bucket 名称，供 `PropertyWeb` 配置使用。

---

### 8. 实施步骤（Implementation Roadmap）

1. **Serverless 组件实现**
   - 创建 S3 bucket 与 CORS 规则。
   - 编写 `GeneratePresignedUrlFunction`：
     - 输入：`ticketId`, `propertyId`, `fileName`, `contentType`, `uploaderId` 等。
     - 逻辑：生成 `tickets/{ticketId}/{guid}-{fileName}` 的预签名 PUT URL（有效期 5 分钟），返回 `uploadUrl`、`key`、`expiresInSeconds`。
   - 通过 API Gateway 暴露 REST 端点，启用 Lambda proxy integration。
   - （可选）实现 `PersistTicketImageMetadataFunction`，将 Lambda 返回的 key 写入 RDS。

2. **`PropertyWeb` 改造**
   - 新增一个 service（例如 `ITicketImageService`）调用 API Gateway 端点。
   - Controller 在处理报修表单提交时：
     1. 请求预签名 URL。
     2. 将 `uploadUrl`、`key` 传给前端。
   - 前端（Razor View + JS）使用 `fetch` 或 `axios` 直接向 S3 `PUT` 文件，上传完成后提示成功。
   - 在现有数据库表中加入字段/新表记录 `s3Key`，并在查看工单详情时加载图片 URL（可通过 CloudFront 或直接 S3 presigned GET）。

3. **安全与配置**
   - 将 API Gateway URL、S3 bucket、Region、API Key 等放入 `appsettings.{Environment}.json` 或 Secrets Manager。
   - 若使用 IAM SigV4 调用 API Gateway，PropertyWeb 需配置 AWS 凭据（ECS/EC2 role 或 AWS CLI profile）。

4. **部署与验证**
   - 通过 IaC 部署 serverless 资源，记录输出（API URL、Bucket 名称）。
   - 在开发环境测试：提交工单 -> 获取预签名 URL -> 浏览器上传 -> S3 验证文件。
   - 编写最小的自动化/单元测试：例如 Lambda handler 针对输入参数的校验。

5. **后续扩展（可选）**
   - S3 事件触发图像处理或 SNS 通知。
   - 使用 CloudFront + Signed URL 提供下载访问。
   - 将 API Gateway 接入 WAF、使用 Cognito 保护接口。

以上步骤完成后，即可证明我们成功把 serverless 架构并入原有系统，同时预留充足空间处理跨账号迁移、自动化与成本优化需求。

