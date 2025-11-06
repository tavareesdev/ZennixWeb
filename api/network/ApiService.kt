package com.zennix.zennix.network

import com.zennix.zennix.model.LoginRequest
import com.zennix.zennix.model.LoginResponse
import retrofit2.Call
import retrofit2.http.Body
import retrofit2.http.POST

interface ApiService {
    @POST("login")
    fun login(@Body request: LoginRequest): Call<LoginResponse>
}
