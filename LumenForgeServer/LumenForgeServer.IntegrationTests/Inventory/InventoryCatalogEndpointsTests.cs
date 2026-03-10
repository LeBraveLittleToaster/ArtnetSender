// InventoryCatalogEndpointsTests.cs
using FluentAssertions;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Inventory;

/// <summary>
/// Integration tests for inventory category and vendor catalog endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class InventoryCatalogEndpointsTests(AuthFixture fixture)
{
    [Fact]
    public async Task GET_categories_requires_authentication()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync("/api/v1/inventory/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_category_as_non_admin_returns_forbidden()
    {
        var testUser = CreateTestUserDto.CreateTestUser();
        var nonAdmin = await fixture.CreateNewUserAsync(testUser);

        var response = await nonAdmin.AppClient.PutAsJsonAsync("/api/v1/inventory/categories", new CreateCategoryDto
        {
            Name = "NoAdminCategory-" + Guid.NewGuid(),
            Description = "Should fail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Category_crud_and_search_flow_works()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var categoryName = "Category-" + Guid.NewGuid();

        var createResponse = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/categories", new CreateCategoryDto
        {
            Name = categoryName,
            Description = "Description " + Guid.NewGuid()
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await InventoryTestHelpers.DeserializeResponseAsync<CategoryView>(createResponse);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/categories/{created.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await admin.AppClient.GetAsync(
            $"/api/v1/inventory/categories?search={Uri.EscapeDataString(categoryName)}&limit=10&offset=0");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listed = await InventoryTestHelpers.DeserializeResponseAsync<List<CategoryView>>(listResponse);
        listed.Should().Contain(c => c.Guid == created.Guid);

        var patchResponse = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/inventory/categories/{created.Guid}",
            new UpdateCategoryDto
            {
                Name = categoryName + "-Updated",
                Description = "Updated description"
            });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await InventoryTestHelpers.DeserializeResponseAsync<CategoryView>(patchResponse);
        updated.Name.Should().EndWith("-Updated");
        updated.Description.Should().Be("Updated description");

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/inventory/categories/{created.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/categories/{created.Guid}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_category_with_duplicate_name_returns_conflict()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var categoryName = "DuplicateCategory-" + Guid.NewGuid();

        var firstCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/categories", new CreateCategoryDto
        {
            Name = categoryName,
            Description = "Description A"
        });
        firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/categories", new CreateCategoryDto
        {
            Name = categoryName,
            Description = "Description B"
        });
        secondCreate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PATCH_category_empty_payload_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var category = await InventoryTestHelpers.CreateCategoryAsync(admin);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/categories/{category.Guid}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_category_not_found_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/inventory/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_categories_invalid_limit_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/inventory/categories?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_vendors_requires_authentication()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync("/api/v1/inventory/vendors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_vendor_as_non_admin_returns_forbidden()
    {
        var testUser = CreateTestUserDto.CreateTestUser();
        var nonAdmin = await fixture.CreateNewUserAsync(testUser);

        var response = await nonAdmin.AppClient.PutAsJsonAsync("/api/v1/inventory/vendors", new CreateVendorDto
        {
            Name = "NoAdminVendor-" + Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Vendor_crud_and_search_flow_works()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendorName = "Vendor-" + Guid.NewGuid();

        var createResponse = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/vendors", new CreateVendorDto
        {
            Name = vendorName
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await InventoryTestHelpers.DeserializeResponseAsync<VendorView>(createResponse);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/vendors/{created.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await admin.AppClient.GetAsync(
            $"/api/v1/inventory/vendors?search={Uri.EscapeDataString(vendorName)}&limit=10&offset=0");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listed = await InventoryTestHelpers.DeserializeResponseAsync<List<VendorView>>(listResponse);
        listed.Should().Contain(v => v.Guid == created.Guid);

        var patchResponse = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/inventory/vendors/{created.Guid}",
            new UpdateVendorDto
            {
                Name = vendorName + "-Updated"
            });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await InventoryTestHelpers.DeserializeResponseAsync<VendorView>(patchResponse);
        updated.Name.Should().EndWith("-Updated");

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/inventory/vendors/{created.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/vendors/{created.Guid}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_vendor_with_duplicate_name_returns_conflict()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendorName = "DuplicateVendor-" + Guid.NewGuid();

        var firstCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/vendors", new CreateVendorDto
        {
            Name = vendorName
        });
        firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/vendors", new CreateVendorDto
        {
            Name = vendorName
        });
        secondCreate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PATCH_vendor_missing_name_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/vendors/{vendor.Guid}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_vendor_not_found_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/inventory/vendors/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_vendors_invalid_limit_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/inventory/vendors?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}