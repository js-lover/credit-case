# Git Workflow — Credit Case

Branch stratejisi: `main → develop → feature/*`  
Commit standardı: Conventional Commits  

> **Kullanım:** Her adımı sırayla, aralıklı oturumlar halinde uygula.  
> `claude.md` dosyasını **hiçbir zaman** commit'e ekleme.

---
<!-- 
## AŞAMA 0 — develop branch'ini oluştur

```bash
git checkout main
git checkout -b develop
git push -u origin develop
``` -->

---
<!-- 
## AŞAMA 1 — feature/domain-model

### Branch oluştur

```bash
git checkout develop
git checkout -b feature/domain-model
```

### Commit 1 — Domain entity'leri ve enum'ları tanımla

```bash
git add CreditCase.Domain/Entities/Customer.cs
git add CreditCase.Domain/Entities/Loan.cs
git add CreditCase.Domain/Entities/Installment.cs
git add CreditCase.Domain/Entities/Payment.cs
git add CreditCase.Domain/Enums/LoanType.cs
git add CreditCase.Domain/Enums/LoanStatus.cs
git add CreditCase.Domain/Enums/InstallmentStatus.cs
git add CreditCase.Domain/Enums/PaymentStatus.cs
git add CreditCase.Domain/Class1.cs
git add CreditCase.Domain/Entities/TestEntity.cs

git commit -m "feat: define core domain entities and enums"
```

### develop'a merge et

```bash
git checkout develop
git merge --no-ff feature/domain-model -m "Merge branch 'feature/domain-model' into develop"
git push origin develop
``` -->

<!-- ---

## AŞAMA 2 — feature/application-layer

### Branch oluştur

```bash
git checkout develop
git checkout -b feature/application-layer
```

### Commit 1 — DTO'lar, interface'ler ve exception tipleri

```bash
git add CreditCase.Application/DTOs/
git add CreditCase.Application/Interfaces/
git add CreditCase.Application/Exceptions/
git add CreditCase.Application/Class1.cs

git commit -m "feat: add DTOs and interfaces for all domain resources"
```

### Commit 2 — Application servisleri (business logic)

```bash
git add CreditCase.Application/Services/CustomerService.cs
git add CreditCase.Application/Services/LoanService.cs
git add CreditCase.Application/Services/InstallmentService.cs
git add CreditCase.Application/Services/PaymentService.cs

git commit -m "feat: implement application services with banking business logic"
```

### Commit 3 — Validation ve DI kurulumu

```bash
git add CreditCase.Application/Validators/
git add CreditCase.Application/DependencyInjection.cs
git add CreditCase.Application/CreditCase.Application.csproj

git commit -m "feat: add FluentValidation validators and dependency injection setup"
```

### develop'a merge et

```bash
git checkout develop
git merge --no-ff feature/application-layer -m "Merge branch 'feature/application-layer' into develop"
git push origin develop
```

--- -->

<!-- ## AŞAMA 3 — feature/infrastructure-layer

### Branch oluştur

```bash
git checkout develop
git checkout -b feature/infrastructure-layer
```

### Commit 1 — AppDbContext ve EF Core yapılandırması

```bash
git add CreditCase.Infrastructure/Persistence/AppDbContext.cs
git add CreditCase.Infrastructure/CreditCase.Infrastructure.csproj
git add CreditCase.Infrastructure/Class1.cs
git add CreditCase.Infrastructure/Data/AppDbContext.cs

git commit -m "feat: configure AppDbContext with EF Core entity mappings"
```

### Commit 2 — Repository implementasyonları

```bash
git add CreditCase.Infrastructure/Persistence/Repositories/CustomerRepository.cs
git add CreditCase.Infrastructure/Persistence/Repositories/LoanRepository.cs
git add CreditCase.Infrastructure/Persistence/Repositories/InstallmentRepository.cs
git add CreditCase.Infrastructure/Persistence/Repositories/PaymentRepository.cs

git commit -m "feat: implement repository pattern for all domain entities"
```

### Commit 3 — Mock dış servis ve DI kurulumu

```bash
git add CreditCase.Infrastructure/Services/MockCreditScoreService.cs
git add CreditCase.Infrastructure/DependencyInjection.cs
git add CreditCase.Infrastructure/AppDbContextFactory.cs

git commit -m "feat: add mock credit score service and infrastructure DI setup"
```

### Commit 4 — EF Core migration

```bash
git add CreditCase.Infrastructure/Migrations/

git commit -m "chore: add initial EF Core database migration for domain schema"
```

### develop'a merge et

```bash
git checkout develop
git merge --no-ff feature/infrastructure-layer -m "Merge branch 'feature/infrastructure-layer' into develop"
git push origin develop
```

--- -->

## AŞAMA 4 — feature/api-layer

### Branch oluştur

```bash
git checkout develop
git checkout -b feature/api-layer
```

<!-- ### Commit 1 — RESTful controller'lar

```bash
git add CreditCase.Api/Controllers/CustomersController.cs
git add CreditCase.Api/Controllers/LoansController.cs
git add CreditCase.Api/Controllers/InstallmentsController.cs
git add CreditCase.Api/Controllers/PaymentsController.cs

git commit -m "feat: add RESTful controllers for all domain resources"
```

### Commit 2 — Global exception handling middleware

```bash
git add CreditCase.Api/Middleware/ExceptionHandlingMiddleware.cs

git commit -m "feat: implement global exception handling middleware"
``` -->

<!-- ### Commit 3 — Program.cs ve DI kurulumu

```bash
git add CreditCase.Api/Program.cs

git commit -m "chore: configure dependency injection and clean up program startup"
```

### develop'a merge et

```bash
git checkout develop
git merge --no-ff feature/api-layer -m "Merge branch 'feature/api-layer' into develop"
git push origin develop
```

--- -->

<!-- ## AŞAMA 4.5 — feature/customer-summary-endpoint

### Branch oluştur

```bash
git checkout develop
git checkout -b feature/customer-summary-endpoint
```

### Commit 1 — Müşteri borç özeti DTO'su ve servis sözleşmesi

```bash
git add CreditCase.Application/DTOs/Customers/CustomerSummaryResponse.cs
git add CreditCase.Application/Interfaces/Repositories/ICustomerRepository.cs
git add CreditCase.Application/Interfaces/Services/ICustomerService.cs

git commit -m "feat: add customer debt summary DTO and service contract"
```

### Commit 2 — Repository ve servis implementasyonu

```bash
git add CreditCase.Infrastructure/Persistence/Repositories/CustomerRepository.cs
git add CreditCase.Application/Services/CustomerService.cs

git commit -m "feat: implement customer summary query with loan and installment aggregation"
```

### Commit 3 — Controller action

```bash
git add CreditCase.Api/Controllers/CustomersController.cs

git commit -m "feat: expose GET /api/customers/{id}/summary endpoint"
```

### develop'a merge et

```bash
git checkout develop
git merge --no-ff feature/customer-summary-endpoint -m "Merge branch 'feature/customer-summary-endpoint' into develop"
git push origin develop
```

--- -->

## AŞAMA 5 — develop → main (release)

```bash
git checkout main
git merge --no-ff develop -m "Merge branch 'develop': complete digital loan management system"
git push origin main
```

---

## Kontrol: Beklenen commit geçmişi (main'de)

```
*   Merge branch 'develop': complete digital loan management system
|\
| *   Merge branch 'feature/api-layer' into develop
| |\
| | * chore: configure dependency injection and clean up program startup
| | * feat: implement global exception handling middleware
| | * feat: add RESTful controllers for all domain resources
| |/
| *   Merge branch 'feature/infrastructure-layer' into develop
| |\
| | * chore: add initial EF Core database migration for domain schema
| | * feat: add mock credit score service and infrastructure DI setup
| | * feat: implement repository pattern for all domain entities
| | * feat: configure AppDbContext with EF Core entity mappings
| |/
| *   Merge branch 'feature/application-layer' into develop
| |\
| | * feat: add FluentValidation validators and dependency injection setup
| | * feat: implement application services with banking business logic
| | * feat: add DTOs and interfaces for all domain resources
| |/
| *   Merge branch 'feature/domain-model' into develop
| |\
| | * feat: define core domain entities and enums
| |/
* chore: initialize backend architecture with SQL Server, EF Core and Swagger setup
```
