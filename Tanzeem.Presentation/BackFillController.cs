//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Tanzeem.Domain.Contracts;
//using Tanzeem.Domain.Entities.Branches;
//using Tanzeem.Domain.Entities.Companies;
//using Tanzeem.Domain.Entities.DeliveryIssues;
//using Tanzeem.Domain.Entities.Orders;
//using Tanzeem.Domain.Entities.Suppliers;
//// ماتنسيش تعملي using للموديلز بتاعتك والـ IUnitOfWork

//[ApiController]
//[Route("api/[controller]")]
//public class BackfillController : ControllerBase
//{
//    private readonly IUnitOfWork _unitOfWork;

//    public BackfillController(IUnitOfWork unitOfWork)
//    {
//        _unitOfWork = unitOfWork;
//    }

//    [HttpPost("fix-orders")]
//    public async Task<IActionResult> FixOrders()
//    {
//        var branches = await _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable().ToListAsync();
//        int totalUpdated = 0;

//        foreach (var branch in branches)
//        {
//            var branchOrders = await _unitOfWork.GetRepository<Order>()
//                .GetAllAsIQueryable()
//                .Where(o => o.BranchId == branch.Id && o.OrderNumber == "Old-Record")
//                .OrderBy(o => o.Id)
//                .ToListAsync();

//            int counter = 1;
//            foreach (var order in branchOrders)
//            {
//                order.OrderNumber = $"ORD-{counter:D4}";
//                _unitOfWork.GetRepository<Order>().UpdateAsync(order);
//                counter++;
//                totalUpdated++;
//            }
//        }

//        await _unitOfWork.SaveChangesAsync();
//        return Ok(new { message = $"تم تسوية وتسلسل عدد {totalUpdated} أوردر قديم بنجاح." });
//    }

//    [HttpPost("fix-delivery-issues")]
//    public async Task<IActionResult> FixDeliveryIssues()
//    {
//        var branches = await _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable().ToListAsync();
//        int totalUpdated = 0;

//        foreach (var branch in branches)
//        {
//            var branchIssues = await _unitOfWork.GetRepository<DeliveryIssue>()
//                .GetAllAsIQueryable()
//                .Where(d => d.BranchId == branch.Id && d.DeliveryIssueNumber == "Old-Record")
//                .OrderBy(d => d.Id)
//                .ToListAsync();

//            int counter = 1;
//            foreach (var issue in branchIssues)
//            {
//                issue.DeliveryIssueNumber = $"ISS-{counter:D4}";
//                _unitOfWork.GetRepository<DeliveryIssue>().UpdateAsync(issue);
//                counter++;
//                totalUpdated++;
//            }
//        }

//        await _unitOfWork.SaveChangesAsync();
//        return Ok(new { message = $"تم تسوية وتسلسل عدد {totalUpdated} إشعار مشكلة قديم بنجاح." });
//    }

//    [HttpPost("fix-suppliers")]
//    public async Task<IActionResult> FixSuppliers()
//    {
//        // 1. نجيب كل الشركات اللي في الداتابيز
//        var companies = await _unitOfWork.GetRepository<Company>().GetAllAsIQueryable().ToListAsync();
//        int totalUpdated = 0;

//        foreach (var company in companies)
//        {
//            // 2. نجيب الموردين التابعين للشركة دي بس، واللي لسه واخدين القيمة القديمة
//            var companySuppliers = await _unitOfWork.GetRepository<Supplier>()
//                .GetAllAsIQueryable()
//                .Where(s => s.CompanyId == company.Id && s.SupplierNumber == "Old-Record")
//                .OrderBy(s => s.Id)
//                .ToListAsync();

//            int counter = 1; // العداد هيبدأ من 1 ويصفر مع كل شركة جديدة

//            foreach (var supplier in companySuppliers)
//            {
//                // 3. التنسيق هيكون مثلاً: SUP1-0001 للشركة رقم 1
//                supplier.SupplierNumber = $"SUP-{counter:D4}";

//                _unitOfWork.GetRepository<Supplier>().UpdateAsync(supplier);
//                // ملاحظة: لو الميثود عندك اسمها UpdateAsync زي ما كتبتي استخدميها عادي

//                counter++;
//                totalUpdated++;
//            }
//        }

//        // 4. نحفظ التعديلات كلها مرة واحدة
//        await _unitOfWork.SaveChangesAsync();

//        return Ok(new { message = $"تم تسوية وتسلسل عدد {totalUpdated} مورد قديم بنجاح." });
//    }
//}