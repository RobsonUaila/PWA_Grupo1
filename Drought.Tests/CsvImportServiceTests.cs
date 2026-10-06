using System.Text;
using Drought.Domain.Entities;
using Drought.Infrastructure.Data;
using Drought.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Drought.Tests;

public class CsvImportServiceTests
{
    private static DroughtDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DroughtDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new DroughtDbContext(options);
        db.Regions.Add(new Region { Id = 1, Name = "Chókwè", Province = "Gaza" });
        db.Regions.Add(new Region { Id = 2, Name = "Massinga", Province = "Inhambane" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Import_ValidRows_AreImported()
    {
        var db = CreateContext();
        var service = new CsvImportService(db);
        var csv = "Date,Region,S1_SoilMoisture,ASCAT_SM,Clay,Sand,OrganicCarbon,BulkDensity,DroughtClass\n" +
                  "2026-01-15,Chókwè,0.18,0.20,34,42,1.8,1.3,SecaSevera\n" +
                  "2026-01-30,Massinga,0.31,0.29,22,55,1.1,1.2,SecaModerada\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportAsync(stream, "teste.csv", "user-1");

        Assert.True(result.Success);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Rejected);
        Assert.Equal(2, await db.SoilObservations.CountAsync());
    }

    [Fact]
    public async Task Import_RejectsOutOfRangeSoilMoisture()
    {
        var db = CreateContext();
        var service = new CsvImportService(db);
        // S1_SoilMoisture = 1.5 está fora do intervalo plausível [0,1].
        var csv = "Date,Region,S1_SoilMoisture,ASCAT_SM,Clay,Sand,OrganicCarbon,BulkDensity,DroughtClass\n" +
                  "2026-01-15,Chókwè,1.5,0.20,34,42,1.8,1.3,SecaSevera\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportAsync(stream, "teste.csv", "user-1");

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Rejected);
    }

    [Fact]
    public async Task Import_RejectsUnknownRegion()
    {
        var db = CreateContext();
        var service = new CsvImportService(db);
        var csv = "Date,Region,S1_SoilMoisture,ASCAT_SM,Clay,Sand,OrganicCarbon,BulkDensity,DroughtClass\n" +
                  "2026-01-15,Maputo,0.20,0.20,34,42,1.8,1.3,Normal\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportAsync(stream, "teste.csv", "user-1");

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Rejected);
        Assert.Contains("não reconhecida", result.Errors[0]);
    }

    [Fact]
    public async Task Import_RejectsDuplicateObservation()
    {
        var db = CreateContext();
        var service = new CsvImportService(db);
        var csv = "Date,Region,S1_SoilMoisture,ASCAT_SM,Clay,Sand,OrganicCarbon,BulkDensity,DroughtClass\n" +
                  "2026-01-15,Chókwè,0.18,0.20,34,42,1.8,1.3,SecaSevera\n" +
                  "2026-01-15,Chókwè,0.19,0.21,34,42,1.8,1.3,SecaSevera\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportAsync(stream, "teste.csv", "user-1");

        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.Rejected);
    }
}
