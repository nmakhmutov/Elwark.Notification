package com.elwark.notification.email

class EmailProviderFactory(
    private val balanceService: EmailBalanceService,
    private val provides: List<IEmailProvider>
) {

    suspend fun get(): IEmailProvider {
        val type = balanceService.getNext()

        return provides.first { it.provider == type }
    }
}