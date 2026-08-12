"""Envio de e-mail transacional — espelho de SmtpEmailSender.cs.

Sem host configurado, apenas loga (modo dev/staging) e retorna sucesso.
"""

from __future__ import annotations

import logging
import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.utils import formataddr

logger = logging.getLogger("openpc_scraper.email")


class EmailSender:
    def __init__(
        self,
        host: str | None = None,
        port: int = 587,
        username: str | None = None,
        password: str | None = None,
        from_: str = "OpenPC <no-reply@openpc.example>",
    ) -> None:
        self._host = host
        self._port = port
        self._username = username
        self._password = password
        self._from = from_

    def send(self, to: str, subject: str, html_body: str) -> None:
        if not self._host:
            logger.info("[email:dry-run] para=%s assunto=%s corpo=%s", to, subject, html_body)
            return

        msg = MIMEMultipart("alternative")
        msg["From"] = formataddr((self._from, self._from)) if "<" not in self._from else self._from
        msg["To"] = to
        msg["Subject"] = subject
        msg.attach(MIMEText(html_body, "html", "utf-8"))

        with smtplib.SMTP(self._host, self._port, timeout=15) as smtp:
            smtp.starttls()
            if self._username:
                smtp.login(self._username, self._password or "")
            smtp.send_message(msg)
