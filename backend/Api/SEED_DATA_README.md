# Seed Data Information

## SuperAdmin Account

The seed data creates a SuperAdmin user with full system access and all permissions.

### Login Credentials:
- **Email:** `admin@jst.com`
- **Password:** `Admin123!`

### What Gets Seeded:

1. **Roles:**
   - SuperAdmin (with all permissions)
   - Admin
   - Manager
   - Staff
   - Customer

2. **Permissions:**
   - All CRUD permissions for: Products, Categories, Users, Roles, Inventory, Orders, Sales, Warehouses, Reports, PromoCodes
   - SuperAdmin role automatically gets all permissions assigned

3. **SuperAdmin User:**
   - Full Name: "Super Administrator"
   - Email: admin@jst.com
   - Password: Admin123!
   - Role: SuperAdmin (with all permissions)

4. **Sample Categories:**
   - Suits
   - Blazers
   - Shirts
   - Accessories

## How to Use:

1. Start your .NET backend API
2. The seed data will automatically run on first startup
3. Login to the admin dashboard at `/admin` using the credentials above
4. You'll have full access to all features and can test the permission system

## Notes:

- Seed data only runs if the database is empty (checks for existing data before seeding)
- To re-seed, delete the database file (`jst.db` or `jst_dev.db`) and restart the API
- The SuperAdmin role bypasses all permission checks in the backend






