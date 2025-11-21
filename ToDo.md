## Task 1 – MVP & Deployment
- [ ] Finalize user stories for owner, admin, worker roles
- [x] Design DB schema (`Users`, `Properties`, `RepairTickets`, optional `PaymentRecords`)
- [x] Scaffold ASP.NET Core backend with authentication/authorization
- [x] Implement role-based portals (owner, admin, worker)
- [ ] Add repair ticket submission with image upload (S3-ready)
- [x] Configure AWS RDS connection via `.env`
- [x] Build responsive HTML/CSS/JS frontend
- [x] Provision SQL Server RDS + apply EF Core migrations
- [x] Deploy app to Elastic Beanstalk or EC2
- [ ] Capture required screenshots (DB creation, deployment, UI)
- [ ] Prepare Task 1 report + source ZIP + demo script

> Note: The Elastic Beanstalk environment is currently running the app on Linux using an in‑memory database because `Microsoft.Data.SqlClient` is not supported on this platform. The full RDS SQL Server integration is verified from the local .NET 7 environment and documented in the Task 1 report.

## Task 2 – Serverless & Monitoring
- [ ] Draft new hybrid architecture diagram (monolith vs serverless)
- [ ] Implement API Gateway + Lambda flow (e.g., submit repair ticket)
- [ ] Integrate at least one service (S3/SNS/SQS) into workflow
- [ ] Enable CloudWatch/X-Ray monitoring for app + Lambda
- [ ] Update user manual with new features
- [ ] Document cloud integrations with code/config screenshots
- [ ] Compile performance analysis + monitoring dashboard captures
- [ ] Collect team reflections & workload matrix
- [ ] Assemble final Word report (<=60 pages)

