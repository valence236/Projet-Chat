package com.chat.app.repository;

import com.chat.app.model.Message;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

public interface MessageRepository extends JpaRepository<Message, Long> {

    // Exemple pour récupérer l'historique d'une conversation privée
    @Query("SELECT m FROM Message m WHERE (m.sender.id = :userId1 AND m.recipient.id = :userId2) OR (m.sender.id = :userId2 AND m.recipient.id = :userId1) ORDER BY m.timestamp ASC")
    List<Message> findConversationHistory(@Param("userId1") Long userId1, @Param("userId2") Long userId2);

    // Historique d'un salon public
    List<Message> findByChannelIdOrderByTimestampAsc(Long channelId);

    // Historique public générique (ni privé, ni salon)
    List<Message> findByRecipientIsNullAndChannelIsNullOrderByTimestampAsc();

    @Query("SELECT CASE WHEN COUNT(m) > 0 THEN true ELSE false END FROM Message m WHERE m.id = :messageId AND m.channel.id = :channelId")
    boolean existsByIdAndChannelId(@Param("messageId") Long messageId, @Param("channelId") Long channelId);

    // Ajoutez d'autres méthodes si nécessaire (ex: pour des salons publics/groupes)
    // List<Message> findByChannelIdOrderByTimestampAsc(Long channelId); 

    @Modifying
    @Transactional
    @Query("DELETE FROM Message m WHERE m.channel.id = :channelId")
    void deleteByChannelId(@Param("channelId") Long channelId);
} 