-- Add ticket_messages table for chat functionality
IF OBJECT_ID('dbo.ticket_messages', 'U') IS NOT NULL 
    DROP TABLE dbo.ticket_messages;

CREATE TABLE dbo.ticket_messages
(
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ticket_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    message_text NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_ticket_messages_ticket FOREIGN KEY (ticket_id) REFERENCES dbo.repair_tickets(id) ON DELETE CASCADE,
    CONSTRAINT fk_ticket_messages_user FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE NO ACTION
);

-- Create index for faster queries
CREATE INDEX idx_ticket_messages_ticket_id ON dbo.ticket_messages(ticket_id);
CREATE INDEX idx_ticket_messages_created_at ON dbo.ticket_messages(created_at);

PRINT 'ticket_messages table created successfully.';










