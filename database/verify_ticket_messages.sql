-- Verify ticket_messages table exists and check its structure
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_NAME = 'ticket_messages';

-- Check table columns
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_NAME = 'ticket_messages'
ORDER BY ORDINAL_POSITION;

-- Check indexes
SELECT 
    i.name AS IndexName,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.ticket_messages')
ORDER BY i.name, ic.key_ordinal;

-- Check foreign keys
SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ReferencedTable,
    cp.name AS ReferencedColumn,
    tr.name AS ReferencingTable,
    cr.name AS ReferencingColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables tp ON fkc.referenced_object_id = tp.object_id
INNER JOIN sys.columns cp ON fkc.referenced_object_id = cp.object_id AND fkc.referenced_column_id = cp.column_id
INNER JOIN sys.tables tr ON fkc.parent_object_id = tr.object_id
INNER JOIN sys.columns cr ON fkc.parent_object_id = cr.object_id AND fkc.parent_column_id = cr.column_id
WHERE tr.name = 'ticket_messages';










