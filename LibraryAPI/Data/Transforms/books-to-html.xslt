<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" indent="yes"/>
    
    <xsl:template name="format-date">
        <xsl:param name="date" select="//GeneratedDate"/>
        <xsl:value-of select="concat(
            substring($date, 1, 4), '-',
            substring($date, 6, 2), '-',
            substring($date, 9, 2), ' ',
            substring($date, 12, 2), ':',
            substring($date, 15, 2), ':',
            substring($date, 18, 2)
        )"/>
    </xsl:template>
    
    <xsl:template match="/">
        <html>
            <head>
                <title>Library Management System Report</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        margin: 20px;
                        line-height: 1.6;
                    }
                    .container {
                        max-width: 1200px;
                        margin: 0 auto;
                    }
                    .header {
                        text-align: center;
                        margin-bottom: 30px;
                        padding: 20px;
                        background-color: #f8f9fa;
                        border-radius: 5px;
                    }
                    .summary-grid {
                        display: grid;
                        grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                        gap: 20px;
                        margin-bottom: 30px;
                    }
                    .summary-card {
                        background-color: #fff;
                        padding: 20px;
                        border-radius: 5px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }
                    .summary-card h3 {
                        margin-top: 0;
                        color: #333;
                        font-size: 1.2em;
                    }
                    .summary-card .value {
                        font-size: 2em;
                        font-weight: bold;
                        color: #007bff;
                        margin: 10px 0;
                    }
                    .section {
                        margin-bottom: 30px;
                        background-color: #fff;
                        padding: 20px;
                        border-radius: 5px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }
                    .section h2 {
                        color: #333;
                        border-bottom: 2px solid #007bff;
                        padding-bottom: 10px;
                        margin-top: 0;
                    }
                    .list-item {
                        display: flex;
                        justify-content: space-between;
                        padding: 10px 0;
                        border-bottom: 1px solid #eee;
                    }
                    .list-item:last-child {
                        border-bottom: none;
                    }
                    .list-item .count {
                        font-weight: bold;
                        color: #007bff;
                    }
                    .timestamp {
                        text-align: center;
                        color: #666;
                        font-size: 0.9em;
                        margin-top: 20px;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Library Management System Report</h1>
                        <p class="timestamp">Generated on: <xsl:call-template name="format-date"/></p>
                    </div>

                    <div class="summary-grid">
                        <div class="summary-card">
                            <h3>Total Books</h3>
                            <div class="value"><xsl:value-of select="//TotalBooks"/></div>
                        </div>
                        <div class="summary-card">
                            <h3>Total Members</h3>
                            <div class="value"><xsl:value-of select="//TotalMembers"/></div>
                        </div>
                        <div class="summary-card">
                            <h3>Active Borrowings</h3>
                            <div class="value"><xsl:value-of select="//ActiveBorrowings"/></div>
                        </div>
                        <div class="summary-card">
                            <h3>Available Books</h3>
                            <div class="value"><xsl:value-of select="//AvailableBooks"/></div>
                        </div>
                        <div class="summary-card">
                            <h3>Overdue Books</h3>
                            <div class="value"><xsl:value-of select="//OverdueBooks"/></div>
                        </div>
                    </div>

                    <div class="section">
                        <h2>Popular Genres</h2>
                        <xsl:for-each select="//PopularGenres/*">
                            <div class="list-item">
                                <span class="genre"><xsl:value-of select="Genre"/></span>
                                <span class="count"><xsl:value-of select="Count"/> books</span>
                            </div>
                        </xsl:for-each>
                    </div>

                    <div class="section">
                        <h2>Popular Authors</h2>
                        <xsl:for-each select="//PopularAuthors/*">
                            <div class="list-item">
                                <span class="author"><xsl:value-of select="Author"/></span>
                                <span class="count"><xsl:value-of select="Count"/> books</span>
                            </div>
                        </xsl:for-each>
                    </div>
                </div>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet> 