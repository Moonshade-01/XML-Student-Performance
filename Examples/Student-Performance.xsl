<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes"/>
  <xsl:template match="/performance">
    <html lang="uk"><head><meta charset="utf-8"/><title>Student-Performance — Успішність студентів</title>
      <style>body{font:16px 'Segoe UI',sans-serif;margin:32px;color:#17263b}h1{color:#174b73}table{border-collapse:collapse;width:100%}th,td{border:1px solid #bdcbd6;padding:10px;text-align:left}th{background:#eaf1f6}tr.failed{background:#fff0ee}caption{text-align:left;padding:12px 0}@media print{body{margin:0;font-size:10pt}th,td{padding:5px}}</style>
    </head><body><h1>Успішність студентів</h1>
      <p>Варіант 6. Результати аналізу XML-документа.</p>
      <table><caption>Кількість записів: <xsl:value-of select="count(record)"/></caption>
        <thead><tr><th>П.І.П.</th><th>Факультет</th><th>Кафедра</th><th>Група</th><th>Дисципліна</th><th>Семестр</th><th>Бал</th><th>ECTS</th></tr></thead>
        <tbody><xsl:for-each select="record"><xsl:sort select="FullName"/>
          <tr><xsl:if test="Score &lt; 60"><xsl:attribute name="class">failed</xsl:attribute></xsl:if>
            <td><xsl:value-of select="FullName"/></td><td><xsl:value-of select="Faculty"/></td>
            <td><xsl:value-of select="Department"/></td><td><xsl:value-of select="Group"/></td>
            <td><xsl:value-of select="Discipline"/></td><td><xsl:value-of select="Semester"/></td>
            <td><xsl:value-of select="Score"/></td><td><xsl:choose>
              <xsl:when test="Score &gt;= 90">A</xsl:when><xsl:when test="Score &gt;= 82">B</xsl:when>
              <xsl:when test="Score &gt;= 75">C</xsl:when><xsl:when test="Score &gt;= 64">D</xsl:when>
              <xsl:when test="Score &gt;= 60">E</xsl:when><xsl:when test="Score &gt;= 35">FX</xsl:when>
              <xsl:otherwise>F</xsl:otherwise></xsl:choose></td>
          </tr></xsl:for-each></tbody>
      </table><xsl:if test="not(record)"><p>За обраними умовами записів не знайдено.</p></xsl:if>
    </body></html>
  </xsl:template>
</xsl:stylesheet>
