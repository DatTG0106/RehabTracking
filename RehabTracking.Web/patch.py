import re

with open(r'd:\EXE\RehabTracking\RehabTracking.Web\Migrations\20260628101749_AddECommerceExtended.cs', 'r', encoding='utf-8') as f:
    content = f.read()

keep_tables = ['ProductVariants', 'Reviews', 'Vouchers', 'Payments']

def replace_create(m):
    block = m.group(0)
    match = re.search(r'name:\s*\"([^\"]+)\"', block)
    if match:
        table_name = match.group(1)
        if table_name in keep_tables:
            return block
        else:
            return '            // Skipped CreateTable for ' + table_name
    return block

content = re.sub(r'migrationBuilder\.CreateTable\([\s\S]*?\)(?=;);', replace_create, content)

def replace_index(m):
    block = m.group(0)
    match = re.search(r'table:\s*\"([^\"]+)\"', block)
    if match:
        table_name = match.group(1)
        if table_name in keep_tables:
            return block
        else:
            return '            // Skipped CreateIndex for ' + table_name
    return block

content = re.sub(r'migrationBuilder\.CreateIndex\([\s\S]*?\)(?=;);', replace_index, content)

with open(r'd:\EXE\RehabTracking\RehabTracking.Web\Migrations\20260628101749_AddECommerceExtended.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('Migration patched successfully.')
